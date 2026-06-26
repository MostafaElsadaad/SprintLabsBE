using System.Net;

using Application.Features.Communities.GradesClasses.UpdateClass;

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

namespace Compass.Tests.Features.CommunityGradesClasses;

public class UpdateClassCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ActiveTeacher_RenamesAndMovesClassWithinCommunity()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Teacher);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new UpdateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 1,
            Name = " Updated Class ",
            GradeId = 2
        }, CancellationToken.None);

        result.Name.Should().Be("Updated Class");
        result.GradeId.Should().Be(2);
        var stored = await context.Classes.SingleAsync(x => x.Id == 1);
        stored.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_OtherCommunityGrade_ThrowsNotFoundWithoutMutation()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 1,
            Name = "Should Not Persist",
            GradeId = 3
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stored = await context.Classes.SingleAsync(x => x.Id == 1);
        stored.Name.Should().Be("Class A");
        stored.GradeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeletedClass_ThrowsBadRequest()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 2,
            Name = "Deleted Class Renamed"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_StudentMembership_ThrowsForbidden()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Student);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 1,
            Name = "Denied"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private UpdateClassCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new UpdateClassCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
