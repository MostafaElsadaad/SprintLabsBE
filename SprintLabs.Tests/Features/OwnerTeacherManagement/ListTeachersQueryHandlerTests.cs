using System.Net;

using Application.Features.Communities.Teachers.ListTeachers;

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

public class ListTeachersQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ActiveOwner_ReturnsTeachersForRequestedCommunityOnly()
    {
        await using var context = CreateContext();
        await SeedTeachers(context);
        SetupUsers();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListTeachersQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(x => x.UserId).Should().BeEquivalentTo(new[] { 20L, 21L, 22L });
        result.Should().Contain(x => x.UserId == 20 && x.Status == CommunityUserStatus.Pending.ToString());
        result.Should().Contain(x => x.UserId == 21 && x.Status == CommunityUserStatus.Active.ToString());
        result.Should().Contain(x => x.UserId == 22 && x.Status == CommunityUserStatus.Removed.ToString());
        result.Should().NotContain(x => x.UserId == 23);
    }

    [Fact]
    public async Task Handle_CommunityWithNoTeachers_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        await SeedOwner(context);
        SetupUsers();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ListTeachersQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithoutActiveOwnerRole_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedOwner(context, CommunityUserRole.Teacher);
        SetupUsers();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new ListTeachersQuery
        {
            UserId = 10,
            CommunityId = 1
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private ListTeachersQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new ListTeachersQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<CommunityUser>(context));
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

    private static async Task SeedOwner(
        ApplicationDbContext context,
        CommunityUserRole ownerRole = CommunityUserRole.Owner)
    {
        context.Communities.Add(new Community
        {
            Id = 1,
            Name = "Example School",
            Slug = "example-school",
            Status = CommunityStatus.Active
        });

        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = 1,
            UserId = 10,
            Role = ownerRole,
            Status = CommunityUserStatus.Active
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTeachers(ApplicationDbContext context)
    {
        await SeedOwner(context);
        context.Communities.Add(new Community
        {
            Id = 2,
            Name = "Other School",
            Slug = "other-school",
            Status = CommunityStatus.Active
        });

        context.CommunityUsers.AddRange(
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 20,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-3)
            },
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 21,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2)
            },
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 22,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Removed,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new CommunityUser
            {
                CommunityId = 2,
                UserId = 23,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Active
            },
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 24,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Active
            });

        await context.SaveChangesAsync();
    }
}
