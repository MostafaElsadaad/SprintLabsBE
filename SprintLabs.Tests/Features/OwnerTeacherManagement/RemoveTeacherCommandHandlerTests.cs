using System.Net;

using Application.Features.Communities.Teachers.RemoveTeacher;

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
using Shared.Responses;

namespace Compass.Tests.Features.OwnerTeacherManagement;

public class RemoveTeacherCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Theory]
    [InlineData(CommunityUserStatus.Pending)]
    [InlineData(CommunityUserStatus.Active)]
    public async Task Handle_CountedTeacherMembership_SetsRemovedAndDecrementsUsedTeachers(
        CommunityUserStatus teacherStatus)
    {
        await using var context = CreateContext();
        await SeedMemberships(context, teacherStatus, usedTeachers: 1);
        SetupUsers();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new RemoveTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            TeacherUserId = 20
        }, CancellationToken.None);

        result.Status.Should().Be(CommunityUserStatus.Removed.ToString());
        var membership = await context.CommunityUsers.SingleAsync(x => x.UserId == 20);
        membership.Status.Should().Be(CommunityUserStatus.Removed);
        membership.UpdatedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(0);

        var access = new CommunityAccessService(new BaseRepository<CommunityUser>(context));
        (await access.CanAccessCommunity(20, 1)).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AlreadyRemovedTeacher_DoesNotDecrementAgain()
    {
        await using var context = CreateContext();
        await SeedMemberships(context, CommunityUserStatus.Removed, usedTeachers: 0);
        SetupUsers();
        var handler = CreateHandler(context);

        await handler.Handle(new RemoveTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            TeacherUserId = 20
        }, CancellationToken.None);

        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(0);
    }

    [Fact]
    public async Task Handle_TargetOwnerMembership_RejectsWithoutMutation()
    {
        await using var context = CreateContext();
        await SeedMemberships(context, CommunityUserStatus.Active, usedTeachers: 1, targetRole: CommunityUserRole.Owner);
        SetupUsers();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new RemoveTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            TeacherUserId = 20
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await context.CommunityUsers.SingleAsync(x => x.UserId == 20)).Status.Should().Be(CommunityUserStatus.Active);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserWithoutActiveOwnerRole_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedMemberships(
            context,
            CommunityUserStatus.Active,
            usedTeachers: 1,
            ownerRole: CommunityUserRole.Teacher);
        SetupUsers();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new RemoveTeacherCommand
        {
            UserId = 10,
            CommunityId = 1,
            TeacherUserId = 20
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private RemoveTeacherCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new RemoveTeacherCommandHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<CommunityLicense>(context));
    }

    private void SetupUsers()
    {
        _userServiceMock
            .Setup(x => x.GetCurrentUser(It.IsAny<long>()))
            .ReturnsAsync((long userId) => new UserIdentityResponse
            {
                Id = userId,
                Email = $"user{userId}@example.com",
                Name = $"User {userId}",
                Status = "Active"
            });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedMemberships(
        ApplicationDbContext context,
        CommunityUserStatus teacherStatus,
        int usedTeachers,
        CommunityUserRole ownerRole = CommunityUserRole.Owner,
        CommunityUserRole targetRole = CommunityUserRole.Teacher)
    {
        context.Communities.Add(new Community
        {
            Id = 1,
            Name = "Example School",
            Slug = "example-school",
            Status = CommunityStatus.Active
        });

        context.CommunityUsers.AddRange(
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 10,
                Role = ownerRole,
                Status = CommunityUserStatus.Active
            },
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 20,
                Role = targetRole,
                Status = teacherStatus
            });

        context.CommunityLicenses.Add(new CommunityLicense
        {
            CommunityId = 1,
            MaxTeachers = 5,
            UsedTeachers = usedTeachers
        });

        await context.SaveChangesAsync();
    }
}
