using System.Net;

using Application.Features.Communities.StudentLicenses.UpdateStudentLicense;

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

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.OwnerStudentLicenseManagement;

public class UpdateStudentLicenseCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_PendingLicense_UpdatesEmailAndAssignmentWithoutChangingUsedStudents()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Pending, usedStudents: 1);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = " New@Example.com ",
            GradeId = 2,
            ClassId = 2
        }, CancellationToken.None);

        result.Email.Should().Be("new@example.com");
        result.EmailChangeCount.Should().Be(1);
        result.Grade.Id.Should().Be(2);
        result.Class.Id.Should().Be(2);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SameEmail_DoesNotIncrementEmailChangeCount()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Pending, usedStudents: 1);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = " student@example.com ",
            GradeId = 1,
            ClassId = 1
        }, CancellationToken.None);

        (await context.StudentLicenses.SingleAsync(x => x.Id == 1)).EmailChangeCount.Should().Be(0);
    }

    [Theory]
    [InlineData(StudentLicenseStatus.Active)]
    [InlineData(StudentLicenseStatus.Revoked)]
    public async Task Handle_NonPendingLicenseEmailChange_ThrowsBadRequest(StudentLicenseStatus status)
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, status, usedStudents: status == StudentLicenseStatus.Active ? 1 : 0);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = "new@example.com",
            GradeId = 1,
            ClassId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_EmailChangeLimitReached_ThrowsBadRequest()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Pending, usedStudents: 1, emailChangeLimit: 1, emailChangeCount: 1);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = "new@example.com",
            GradeId = 1,
            ClassId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_DuplicateEmailOrInvalidGradeClass_RejectsWithoutMutation()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Pending, usedStudents: 2);
        context.StudentLicenses.Add(new StudentLicense
        {
            CommunityId = 1,
            Email = "duplicate@example.com",
            GradeId = 1,
            ClassId = 1,
            Status = StudentLicenseStatus.Pending,
            AssignedByUserId = 10
        });
        await context.SaveChangesAsync();
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var duplicate = async () => await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = "duplicate@example.com",
            GradeId = 1,
            ClassId = 1
        }, CancellationToken.None);

        (await duplicate.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var invalidClass = async () => await handler.Handle(new UpdateStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1,
            Email = "student@example.com",
            GradeId = 1,
            ClassId = 2
        }, CancellationToken.None);

        (await invalidClass.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SeedLicense(
        ApplicationDbContext context,
        StudentLicenseStatus status,
        int usedStudents,
        int emailChangeLimit = 2,
        int emailChangeCount = 0)
    {
        await OwnerStudentLicenseManagementTestHelper.SeedBase(
            context,
            usedStudents: usedStudents,
            emailChangeLimit: emailChangeLimit);
        context.StudentLicenses.Add(new StudentLicense
        {
            Id = 1,
            CommunityId = 1,
            Email = "student@example.com",
            GradeId = 1,
            ClassId = 1,
            Status = status,
            EmailChangeCount = emailChangeCount,
            AssignedByUserId = 10
        });
        await context.SaveChangesAsync();
    }

    private UpdateStudentLicenseCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new UpdateStudentLicenseCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<CommunityLicense>(context),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
