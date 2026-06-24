using System.Net;

using Application.Features.Users.GetCurrentUserCommunities;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.CommunityAccessFoundation;

public class GetCurrentUserCommunitiesQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_ReturnsCurrentUsersActiveCommunities()
    {
        await using var context = CreateContext();
        await SeedCommunities(context);

        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "owner@example.com",
                Name = "Owner",
                Status = "Active"
            });

        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetCurrentUserCommunitiesQuery { UserId = 10 }, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new UserCommunityResponse
        {
            CommunityId = 1,
            CommunityName = "Example School",
            Slug = "example-school",
            CommunityStatus = "Active",
            Role = "Owner",
            MembershipStatus = "Active"
        });
    }

    [Fact]
    public async Task Handle_SuspendedUser_ThrowsForbidden()
    {
        await using var context = CreateContext();

        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "owner@example.com",
                Name = "Owner",
                Status = "Suspended",
                IsSuspended = true
            });

        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new GetCurrentUserCommunitiesQuery { UserId = 10 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.ErrorCode.Should().Be(ErrorCode.Failure);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidAccessToken);
    }

    private GetCurrentUserCommunitiesQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new GetCurrentUserCommunitiesQueryHandler(
            _userServiceMock.Object,
            new BaseRepository<CommunityUser>(context));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedCommunities(ApplicationDbContext context)
    {
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Example School", Slug = "example-school", Status = CommunityStatus.Active },
            new Community { Id = 2, Name = "Pending School", Slug = "pending-school", Status = CommunityStatus.Active },
            new Community { Id = 3, Name = "Removed School", Slug = "removed-school", Status = CommunityStatus.Active },
            new Community { Id = 4, Name = "Other School", Slug = "other-school", Status = CommunityStatus.Active });

        context.CommunityUsers.AddRange(
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 10,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Active
            },
            new CommunityUser
            {
                CommunityId = 2,
                UserId = 10,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending
            },
            new CommunityUser
            {
                CommunityId = 3,
                UserId = 10,
                Role = CommunityUserRole.Student,
                Status = CommunityUserStatus.Removed
            },
            new CommunityUser
            {
                CommunityId = 4,
                UserId = 20,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Active
            });

        await context.SaveChangesAsync();
    }
}
