using System.Net;

using Application.Features.Communities.Students.GetStudentDetail;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Moq;

using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityStudentViewing;

public class GetStudentDetailQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_Owner_ReturnsStudentDetailWithPlaceholderAnalytics()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetStudentDetailQuery
        {
            UserId = 10,
            CommunityId = 1,
            PlayerProfileId = 200
        }, CancellationToken.None);

        result.UserId.Should().Be(100);
        result.PlayerProfileId.Should().Be(200);
        result.Name.Should().Be("Active Player");
        result.Email.Should().Be("active.student@example.com");
        result.Gold.Should().Be(7);
        result.Experience.Should().Be(120);
        result.Level.Should().Be(3);
        result.LicenseStatus.Should().Be(StudentLicenseStatus.Active.ToString());
        result.GradeName.Should().Be("7");
        result.ClassName.Should().Be("Class A");
        result.Analytics.CompletedAssignments.Should().Be(0);
        result.Analytics.AverageScore.Should().BeNull();
        result.Analytics.LastActivityAt.Should().BeNull();
        result.Analytics.MatchesPlayed.Should().Be(0);
        result.Analytics.WinRate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Teacher_ReturnsStudentDetail()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context, CommunityUserRole.Teacher);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetStudentDetailQuery
        {
            UserId = 10,
            CommunityId = 1,
            PlayerProfileId = 200
        }, CancellationToken.None);

        result.PlayerProfileId.Should().Be(200);
    }

    [Fact]
    public async Task Handle_PlayerFromAnotherCommunity_ThrowsNotFound()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new GetStudentDetailQuery
        {
            UserId = 10,
            CommunityId = 1,
            PlayerProfileId = 201
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_RevokedOnlyLicense_ThrowsNotFound()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context);
        context.StudentLicenses.Single(x => x.Id == 2).PlayerProfileId = null;
        await context.SaveChangesAsync();
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new GetStudentDetailQuery
        {
            UserId = 10,
            CommunityId = 1,
            PlayerProfileId = 200
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_DisallowedMembership_ThrowsForbidden()
    {
        await using var context = CommunityStudentViewingTestHelper.CreateContext();
        await CommunityStudentViewingTestHelper.SeedRoster(context, CommunityUserRole.Student);
        CommunityStudentViewingTestHelper.SetupUsers(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new GetStudentDetailQuery
        {
            UserId = 10,
            CommunityId = 1,
            PlayerProfileId = 200
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private GetStudentDetailQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new GetStudentDetailQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<StudentLicense>(context),
            new BaseRepository<Player>(context));
    }
}
