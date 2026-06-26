using System.Net;

using Application.Features.Communities.GradesClasses.ListClasses;

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

namespace Compass.Tests.Features.CommunityGradesClasses;

public class ListClassesQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_NoGradeFilter_ReturnsOnlyActiveRouteCommunityClasses()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListClassesQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.Select(x => x.Name).Should().Equal("Class A", "Class B");
    }

    [Fact]
    public async Task Handle_GradeFilter_ReturnsOnlyClassesUnderThatGrade()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Teacher);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListClassesQuery
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 2
        }, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Class B");
    }

    [Fact]
    public async Task Handle_OtherCommunityGradeFilter_ThrowsNotFound()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListClassesQuery
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 3
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private ListClassesQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new ListClassesQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
