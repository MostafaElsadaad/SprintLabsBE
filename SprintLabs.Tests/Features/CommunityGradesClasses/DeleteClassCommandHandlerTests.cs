using System.Net;

using Application.Features.Communities.GradesClasses.DeleteClass;

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

public class DeleteClassCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ActiveOwner_SoftDeletesWithoutHardDelete()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateDeleteHandler(context);

        var result = await handler.Handle(new DeleteClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 1
        }, CancellationToken.None);

        result.Status.Should().Be(ClassStatus.Deleted.ToString());
        var stored = await context.Classes.SingleAsync(x => x.Id == 1);
        stored.Status.Should().Be(ClassStatus.Deleted);
        (await context.Classes.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task Handle_AlreadyDeletedClass_IsIdempotent()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Teacher);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateDeleteHandler(context);

        var result = await handler.Handle(new DeleteClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 2
        }, CancellationToken.None);

        result.Status.Should().Be(ClassStatus.Deleted.ToString());
    }

    [Fact]
    public async Task Handle_StudentMembership_ThrowsForbidden()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Student);
        await CommunityGradesClassesTestHelper.SeedGradesAndClasses(context);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateDeleteHandler(context);

        var act = async () => await handler.Handle(new DeleteClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            ClassId = 1
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private DeleteClassCommandHandler CreateDeleteHandler(ApplicationDbContext context)
    {
        return new DeleteClassCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<ClassEntity>(context));
    }

}
