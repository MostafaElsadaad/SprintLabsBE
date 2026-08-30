using System.Net;

using Application.Features.Communities.GradesClasses.CreateClass;

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

public class CreateClassCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Theory]
    [InlineData(CommunityUserRole.Owner)]
    [InlineData(CommunityUserRole.Teacher)]
    public async Task Handle_ActiveOwnerOrTeacherWithSameCommunityGrade_CreatesClass(
        CommunityUserRole role)
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, role);
        context.Grades.Add(new Grade { Id = 1, CommunityId = 1, Value = 7, Name = "Grade 7", SortOrder = 7 });
        await context.SaveChangesAsync();
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new CreateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 1,
            Name = " Class A "
        }, CancellationToken.None);

        result.Name.Should().Be("Class A");
        result.Status.Should().Be(ClassStatus.Active.ToString());
        (await context.Classes.CountAsync(x => x.CommunityId == 1)).Should().Be(1);
    }

    [Fact]
    public async Task Handle_GradeFromAnotherCommunity_ThrowsNotFoundWithoutCreatingClass()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Owner);
        context.Grades.Add(new Grade { Id = 3, CommunityId = 2, Value = 7, Name = "Grade 7", SortOrder = 7 });
        await context.SaveChangesAsync();
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new CreateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 3,
            Name = "Class A"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await context.Classes.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_StudentMembership_ThrowsForbidden()
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, CommunityUserRole.Student);
        context.Grades.Add(new Grade { Id = 1, CommunityId = 1, Value = 7, Name = "Grade 7", SortOrder = 7 });
        await context.SaveChangesAsync();
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new CreateClassCommand
        {
            UserId = 10,
            CommunityId = 1,
            GradeId = 1,
            Name = "Class A"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private CreateClassCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new CreateClassCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Grade>(context),
            new BaseRepository<ClassEntity>(context));
    }
}
