using Application.Features.Communities.GradesClasses.ListGrades;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Moq;

using ClassEntity = Domain.Models.Class;

namespace Compass.Tests.Features.CommunityGradesClasses;

public class ListGradesQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ReturnsRouteCommunityGradesWithActiveClassCounts()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Teacher);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListGradesQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Equal(1, 2);
        result.Single(x => x.Id == 1).ClassCount.Should().Be(1);
        result.Single(x => x.Id == 2).ClassCount.Should().Be(1);
    }

    private ListGradesQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new ListGradesQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
