using System.Net;

using Application.Features.Communities.GradesClasses.CreateGrade;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Enums;
using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityGradesClasses;

public class CreateGradeCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Theory]
    [InlineData(CommunityUserRole.Owner)]
    [InlineData(CommunityUserRole.Teacher)]
    public async Task Handle_ActiveOwnerOrTeacher_CreatesGrade(CommunityUserRole role)
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, role);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new CreateGradeCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "  Grade 5  ",
            SortOrder = 5
        }, CancellationToken.None);

        result.Name.Should().Be("Grade 5");
        result.SortOrder.Should().Be(5);
        result.ClassCount.Should().Be(0);
        (await context.Grades.CountAsync(x => x.CommunityId == 1)).Should().Be(1);
    }

    [Theory]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Removed)]
    public async Task Handle_UserWithoutActiveOwnerOrTeacherRole_ThrowsForbidden(
        CommunityUserRole role,
        CommunityUserStatus status)
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        await CommunityGradesClassesTestHelper.SeedCommunities(context, role, status);
        CommunityGradesClassesTestHelper.SetupActiveUser(_userServiceMock);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new CreateGradeCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Grade 5",
            SortOrder = 5
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("Grade 5", -1)]
    public async Task Handle_InvalidInput_ThrowsBadRequest(string name, int sortOrder)
    {
        await using var context = CommunityGradesClassesTestHelper.CreateContext();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new CreateGradeCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = name,
            SortOrder = sortOrder
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
    }

    private CreateGradeCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new CreateGradeCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Grade>(context));
    }
}
