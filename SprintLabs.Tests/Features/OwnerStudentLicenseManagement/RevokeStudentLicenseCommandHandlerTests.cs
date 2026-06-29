using System.Net;

using Application.Features.Communities.StudentLicenses.RevokeStudentLicense;

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

namespace Compass.Tests.Features.OwnerStudentLicenseManagement;

public class RevokeStudentLicenseCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Theory]
    [InlineData(StudentLicenseStatus.Pending)]
    [InlineData(StudentLicenseStatus.Active)]
    public async Task Handle_CountedLicense_SetsRevokedAndDecrementsUsedStudents(StudentLicenseStatus status)
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, status, usedStudents: 1);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new RevokeStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1
        }, CancellationToken.None);

        result.Status.Should().Be(StudentLicenseStatus.Revoked.ToString());
        var stored = await context.StudentLicenses.SingleAsync(x => x.Id == 1);
        stored.Status.Should().Be(StudentLicenseStatus.Revoked);
        stored.UpdatedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(0);
        (await context.StudentLicenses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedLicense_DoesNotDecrementAgain()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Revoked, usedStudents: 0);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        await handler.Handle(new RevokeStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1
        }, CancellationToken.None);

        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ActiveLinkedLicense_RemovesOnlyMatchingStudentMembership()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Active, usedStudents: 1, userId: 20);
        context.CommunityUsers.AddRange(
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 20,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active
            },
            new CommunityUser
            {
                CommunityId = 2,
                UserId = 20,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active
            },
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 30,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active
            });
        await context.SaveChangesAsync();
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        await handler.Handle(new RevokeStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1
        }, CancellationToken.None);

        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20 && x.Role == CommunityUserRole.Student))
            .Status.Should().Be(CommunityUserStatus.Removed);
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 2 && x.UserId == 20)).Status.Should().Be(CommunityUserStatus.Active);
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 30)).Status.Should().Be(CommunityUserStatus.Active);
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbiddenWithoutMutation()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicense(context, StudentLicenseStatus.Pending, usedStudents: 1, ownerRole: CommunityUserRole.Teacher);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new RevokeStudentLicenseCommand
        {
            UserId = 10,
            CommunityId = 1,
            LicenseId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await context.StudentLicenses.SingleAsync(x => x.Id == 1)).Status.Should().Be(StudentLicenseStatus.Pending);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedStudents.Should().Be(1);
    }

    private async Task SeedLicense(
        ApplicationDbContext context,
        StudentLicenseStatus status,
        int usedStudents,
        long? userId = null,
        CommunityUserRole ownerRole = CommunityUserRole.Owner)
    {
        await OwnerStudentLicenseManagementTestHelper.SeedBase(
            context,
            ownerRole: ownerRole,
            usedStudents: usedStudents);
        context.StudentLicenses.Add(new StudentLicense
        {
            Id = 1,
            CommunityId = 1,
            Email = "student@example.com",
            UserId = userId,
            GradeId = 1,
            ClassId = 1,
            Status = status,
            AssignedByUserId = 10
        });
        await context.SaveChangesAsync();
    }

    private RevokeStudentLicenseCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new RevokeStudentLicenseCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<CommunityLicense>(context),
            new BaseRepository<CommunityUser>(context));
    }
}
