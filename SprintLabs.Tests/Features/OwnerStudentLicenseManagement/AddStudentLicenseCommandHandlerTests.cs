using System.Net;

using Application.Features.Communities.StudentLicenses.AddStudentLicense;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Exceptions;
using Shared.Responses;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.OwnerStudentLicenseManagement;

public class AddStudentLicenseCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ActiveOwnerWithCapacity_CreatesPendingLicenseAndIncrementsUsedStudents()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        SetupMissingTargetStudent("student@example.com");
        var handler = CreateHandler(context);

        var result = await handler.Handle(new AddStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = " Student@Example.com ",
            GradeId = 1,
            ClassId = 1
        }, CancellationToken.None);

        result.Email.Should().Be("student@example.com");
        result.Status.Should().Be(StudentLicenseStatus.Pending.ToString());
        result.EmailChangeCount.Should().Be(0);
        result.AssignedByUserId.Should().Be(10);

        var license = await context.StudentLicenses.SingleAsync();
        license.Status.Should().Be(StudentLicenseStatus.Pending);
        license.Email.Should().Be("student@example.com");
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
        (await context.CommunityUsers.AnyAsync(x => x.Role == CommunityUserRole.Student)).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_LoggedInStudent_CreatesActiveLicensePlayerProfileAndStudentMembership()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        SetupTargetStudent("student@example.com", userId: 20, googleId: "student-google-id");
        var handler = CreateHandler(context);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Email.Should().Be("student@example.com");
        result.Status.Should().Be(StudentLicenseStatus.Active.ToString());
        result.UserId.Should().Be(20);
        result.PlayerProfileId.Should().NotBeNull();
        result.ActivatedAt.Should().NotBeNull();

        var license = await context.StudentLicenses.SingleAsync();
        license.Status.Should().Be(StudentLicenseStatus.Active);
        license.UserId.Should().Be(20);
        license.PlayerProfileId.Should().NotBeNull();
        license.ActivatedAt.Should().NotBeNull();

        var player = await context.Players.SingleAsync();
        player.UserId.Should().Be(20);
        player.GoogleId.Should().Be("student-google-id");

        var membership = await context.CommunityUsers.SingleAsync(
            x => x.CommunityId == 1 && x.UserId == 20);
        membership.Role.Should().Be(CommunityUserRole.Student);
        membership.Status.Should().Be(CommunityUserStatus.Active);

        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
    }

    [Fact]
    public async Task Handle_LoggedInStudentWithRemovedStudentMembership_RestoresMembership()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = CommunityUserRole.Student,
            Status = CommunityUserStatus.Removed
        });
        await context.SaveChangesAsync();
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        SetupTargetStudent("student@example.com", userId: 20, googleId: "student-google-id");
        var handler = CreateHandler(context);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        (await context.CommunityUsers.CountAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().Be(1);
        var membership = await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20);
        membership.Role.Should().Be(CommunityUserRole.Student);
        membership.Status.Should().Be(CommunityUserStatus.Active);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner)]
    [InlineData(CommunityUserRole.Teacher)]
    public async Task Handle_LoggedInStudentWithExistingNonStudentMembership_RejectsWithoutCreatingLicense(
        CommunityUserRole existingRole)
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 20,
            Role = existingRole,
            Status = CommunityUserStatus.Active
        });
        await context.SaveChangesAsync();
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        SetupTargetStudent("student@example.com", userId: 20, googleId: "student-google-id");
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await context.StudentLicenses.AnyAsync()).Should().BeFalse();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(0);
    }

    [Fact]
    public async Task Handle_FullCapacity_RejectsWithoutCreatingLicense()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context, maxStudents: 1, usedStudents: 1);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await context.StudentLicenses.AnyAsync()).Should().BeFalse();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(1, 3)]
    [InlineData(1, 2)]
    [InlineData(1, 4)]
    public async Task Handle_InvalidGradeClassCombination_ThrowsNotFoundWithoutMutation(
        long gradeId,
        long classId)
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var command = ValidCommand();
        command.GradeId = gradeId;
        command.ClassId = classId;

        var act = async () => await handler.Handle(command, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await context.StudentLicenses.AnyAsync()).Should().BeFalse();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DuplicateNonRevokedEmail_RejectsButRevokedEmailCanBeReused()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context, usedStudents: 1);
        context.StudentLicenses.Add(new StudentLicense
        {
            CommunityId = 1,
            Email = "student@example.com",
            GradeId = 1,
            ClassId = 1,
            Status = StudentLicenseStatus.Pending,
            AssignedByUserId = 10
        });
        context.StudentLicenses.Add(new StudentLicense
        {
            CommunityId = 1,
            Email = "revoked@example.com",
            GradeId = 1,
            ClassId = 1,
            Status = StudentLicenseStatus.Revoked,
            AssignedByUserId = 10
        });
        await context.SaveChangesAsync();
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        SetupMissingTargetStudent("revoked@example.com");
        var handler = CreateHandler(context);

        var duplicate = ValidCommand();
        duplicate.Email = " STUDENT@example.com ";
        var duplicateAct = async () => await handler.Handle(duplicate, CancellationToken.None);

        (await duplicateAct.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var reusable = ValidCommand();
        reusable.Email = "revoked@example.com";
        await handler.Handle(reusable, CancellationToken.None);

        (await context.StudentLicenses.CountAsync(x => x.Email == "revoked@example.com")).Should().Be(2);
    }

    [Theory]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Removed)]
    public async Task Handle_UserWithoutActiveOwnerRole_ThrowsForbidden(
        CommunityUserRole role,
        CommunityUserStatus status)
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context, role, status);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(ValidCommand(), CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static AddStudentLicenseCommand ValidCommand()
    {
        return new AddStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            Email = "student@example.com",
            GradeId = 1,
            ClassId = 1
        };
    }

    private AddStudentLicenseCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new AddStudentLicenseCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<CommunityLicense>(context),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context),
            new BaseRepository<CommunityUser>(context),
            new PlayerRepository(context));
    }

    private void SetupTargetStudent(
        string email,
        long userId = 20,
        string? googleId = null)
    {
        _userServiceMock
            .Setup(x => x.FindByEmail(email))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = userId,
                GoogleId = googleId,
                Email = email,
                Name = "Student Name",
                Status = "Active",
                HasGoogleIdentity = !string.IsNullOrWhiteSpace(googleId)
            });
    }

    private void SetupMissingTargetStudent(string email)
    {
        _userServiceMock
            .Setup(x => x.FindByEmail(email))
            .ReturnsAsync((UserIdentityResponse?)null);
    }
}
