using System.Net;

using Application.Features.Communities.StudentLicenses.ListStudentLicenses;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Moq;

using Shared.Exceptions;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.OwnerStudentLicenseManagement;

public class ListStudentLicensesQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ReturnsOnlyRouteCommunityLicenses()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicenses(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListStudentLicensesQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(x => x.Email).Should().NotContain("other@example.com");
    }

    [Fact]
    public async Task Handle_AppliesStatusGradeClassAndSearchFilters()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicenses(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListStudentLicensesQuery
        {
            UserId = 10,
            CommunityId = 1,
            Status = StudentLicenseStatus.Pending,
            GradeId = 1,
            ClassId = 1,
            Search = "alpha"
        }, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Email.Should().Be("alpha@example.com");
        result[0].Grade.Name.Should().Be("Grade 5");
        result[0].Class.Name.Should().Be("Class A");
    }

    [Fact]
    public async Task Handle_InvalidGradeFilter_ThrowsNotFound()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await SeedLicenses(context);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListStudentLicensesQuery
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 3
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbidden()
    {
        await using var context = OwnerStudentLicenseManagementTestHelper.CreateContext();
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context, CommunityUserRole.Teacher);
        OwnerStudentLicenseManagementTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListStudentLicensesQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task SeedLicenses(ApplicationDbContext context)
    {
        await OwnerStudentLicenseManagementTestHelper.SeedBase(context);
        context.StudentLicenses.AddRange(
            new StudentLicense
            {
                CommunityId = 1,
                Email = "alpha@example.com",
                GradeId = 1,
                ClassId = 1,
                Status = StudentLicenseStatus.Pending,
                AssignedByUserId = 10
            },
            new StudentLicense
            {
                CommunityId = 1,
                Email = "beta@example.com",
                GradeId = 2,
                ClassId = 2,
                Status = StudentLicenseStatus.Active,
                AssignedByUserId = 10
            },
            new StudentLicense
            {
                CommunityId = 1,
                Email = "revoked@example.com",
                GradeId = 1,
                ClassId = 1,
                Status = StudentLicenseStatus.Revoked,
                AssignedByUserId = 10
            },
            new StudentLicense
            {
                CommunityId = 2,
                Email = "other@example.com",
                GradeId = 3,
                ClassId = 3,
                Status = StudentLicenseStatus.Pending,
                AssignedByUserId = 10
            });
        await context.SaveChangesAsync();
    }

    private ListStudentLicensesQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new ListStudentLicensesQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
