using System.Net;

using Application.Features.Communities.GetCommunityProfile;

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
using Shared.Responses;

namespace Compass.Tests.Features.OwnerCommunityProfile;

public class GetCommunityProfileQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICommunityAccessService> _communityAccessServiceMock = new();

    [Theory]
    [InlineData(CommunityUserRole.Owner)]
    [InlineData(CommunityUserRole.Teacher)]
    [InlineData(CommunityUserRole.Student)]
    public async Task Handle_ActiveMember_ReturnsCommunityProfile(CommunityUserRole role)
    {
        await using var context = CreateContext();
        await SeedCommunity(context, role, CommunityUserStatus.Active);
        SetupActiveUser();

        var handler = CreateHandlerWithRealAccessService(context);

        var result = await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        result.Should().BeEquivalentTo(new
        {
            Id = 1L,
            Name = "Example School",
            Slug = "example-school",
            Status = CommunityStatus.Active.ToString()
        });
    }

    [Theory]
    [InlineData(CommunityUserStatus.Pending)]
    [InlineData(CommunityUserStatus.Removed)]
    public async Task Handle_InactiveMembership_ThrowsForbidden(CommunityUserStatus status)
    {
        await using var context = CreateContext();
        await SeedCommunity(context, CommunityUserRole.Owner, status);
        SetupActiveUser();

        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidAccessToken);
    }

    [Fact]
    public async Task Handle_UserWithoutMembership_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedCommunity(context);
        SetupActiveUser();

        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Handle_PlatformAdminWithoutMembership_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedCommunity(context);
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Status = "Active",
                IsPlatformAdmin = true
            });

        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Handle_MissingUser_ThrowsNotFound()
    {
        await using var context = CreateContext();
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync((UserIdentityResponse?)null);

        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _communityAccessServiceMock.Verify(
            x => x.CanAccessCommunity(It.IsAny<long>(), It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SuspendedUser_ThrowsForbiddenWithoutCheckingMembership()
    {
        await using var context = CreateContext();
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Status = "Suspended",
                IsSuspended = true
            });

        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 1 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _communityAccessServiceMock.Verify(
            x => x.CanAccessCommunity(It.IsAny<long>(), It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AccessibleMissingCommunity_ThrowsNotFound()
    {
        await using var context = CreateContext();
        SetupActiveUser();
        _communityAccessServiceMock
            .Setup(x => x.CanAccessCommunity(10, 999))
            .ReturnsAsync(true);

        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new GetCommunityProfileQuery { UserId = 10, CommunityId = 999 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be(ErrorMessage.NotFound);
    }

    private GetCommunityProfileQueryHandler CreateHandler(ApplicationDbContext context)
    {
        return new GetCommunityProfileQueryHandler(
            _userServiceMock.Object,
            _communityAccessServiceMock.Object,
            new BaseRepository<Community>(context));
    }

    private GetCommunityProfileQueryHandler CreateHandlerWithRealAccessService(ApplicationDbContext context)
    {
        return new GetCommunityProfileQueryHandler(
            _userServiceMock.Object,
            new CommunityAccessService(new BaseRepository<CommunityUser>(context)),
            new BaseRepository<Community>(context));
    }

    private void SetupActiveUser()
    {
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "member@example.com",
                Name = "Member",
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

    private static async Task SeedCommunity(
        ApplicationDbContext context,
        CommunityUserRole? role = null,
        CommunityUserStatus status = CommunityUserStatus.Active)
    {
        context.Communities.Add(new Community
        {
            Id = 1,
            Name = "Example School",
            Slug = "example-school",
            Status = CommunityStatus.Active
        });

        if (role.HasValue)
        {
            context.CommunityUsers.Add(new CommunityUser
            {
                CommunityId = 1,
                UserId = 10,
                Role = role.Value,
                Status = status
            });
        }

        await context.SaveChangesAsync();
    }
}
