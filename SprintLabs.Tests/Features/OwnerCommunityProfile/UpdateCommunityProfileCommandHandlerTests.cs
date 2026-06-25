using System.Net;

using Application.Features.Communities.UpdateCommunityProfile;

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

public class UpdateCommunityProfileCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ICommunityAccessService> _communityAccessServiceMock = new();

    [Fact]
    public async Task Handle_ActiveOwner_UpdatesAndNormalizesCommunityProfile()
    {
        await using var context = CreateContext();
        await SeedCommunities(context, CommunityUserRole.Owner, CommunityUserStatus.Active);
        SetupActiveUser();
        var handler = CreateHandlerWithRealAccessService(context);

        var result = await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "  Updated School  ",
            Slug = "  Updated-School  "
        }, CancellationToken.None);

        result.Name.Should().Be("Updated School");
        result.Slug.Should().Be("updated-school");
        result.Status.Should().Be(CommunityStatus.Active.ToString());

        var stored = await context.Communities.SingleAsync(x => x.Id == 1);
        stored.Name.Should().Be("Updated School");
        stored.Slug.Should().Be("updated-school");
        stored.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CurrentCommunitySlug_IsAllowed()
    {
        await using var context = CreateContext();
        await SeedCommunities(context, CommunityUserRole.Owner, CommunityUserStatus.Active);
        SetupActiveUser();
        var handler = CreateHandlerWithRealAccessService(context);

        var result = await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Renamed School",
            Slug = "  EXAMPLE-SCHOOL "
        }, CancellationToken.None);

        result.Name.Should().Be("Renamed School");
        result.Slug.Should().Be("example-school");
    }

    [Fact]
    public async Task Handle_SlugOwnedByAnotherCommunity_RejectsWithoutMutation()
    {
        await using var context = CreateContext();
        await SeedCommunities(context, CommunityUserRole.Owner, CommunityUserStatus.Active);
        SetupActiveUser();
        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Should Not Persist",
            Slug = "OTHER-SCHOOL"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be(ErrorMessage.ExistingRecord);

        var stored = await context.Communities.SingleAsync(x => x.Id == 1);
        stored.Name.Should().Be("Example School");
        stored.Slug.Should().Be("example-school");
        stored.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Removed)]
    public async Task Handle_UserWithoutActiveOwnerRole_ThrowsForbidden(
        CommunityUserRole currentRole,
        CommunityUserStatus currentStatus)
    {
        await using var context = CreateContext();
        await SeedCommunities(context, currentRole, currentStatus);
        SetupActiveUser();
        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Denied",
            Slug = "denied"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var stored = await context.Communities.SingleAsync(x => x.Id == 1);
        stored.Name.Should().Be("Example School");
    }

    [Fact]
    public async Task Handle_PlatformAdminWithoutMembership_ThrowsForbidden()
    {
        await using var context = CreateContext();
        await SeedCommunities(context);
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Status = "Active",
                IsPlatformAdmin = true
            });
        var handler = CreateHandlerWithRealAccessService(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Denied",
            Slug = "denied"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Handle_AuthorizedOwnerForMissingCommunity_ThrowsNotFound()
    {
        await using var context = CreateContext();
        SetupActiveUser();
        _communityAccessServiceMock
            .Setup(x => x.HasCommunityRole(
                10,
                999,
                It.Is<IEnumerable<CommunityUserRole>>(roles =>
                    roles.SequenceEqual(new[] { CommunityUserRole.Owner }))))
            .ReturnsAsync(true);
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 999,
            Name = "Missing",
            Slug = "missing"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be(ErrorMessage.NotFound);
    }

    [Fact]
    public async Task Handle_SuspendedUser_ThrowsForbiddenWithoutCheckingRole()
    {
        await using var context = CreateContext();
        await SeedCommunities(context);
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Status = "Suspended",
                IsSuspended = true
            });
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = "Denied",
            Slug = "denied"
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _communityAccessServiceMock.Verify(
            x => x.HasCommunityRole(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<CommunityUserRole>>()),
            Times.Never);
    }

    [Theory]
    [InlineData("", "valid-slug")]
    [InlineData("Valid Name", " ")]
    public async Task Handle_EmptyProfileField_ThrowsBadRequest(string name, string slug)
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(new UpdateCommunityProfileCommand
        {
            UserId = 10,
            CommunityId = 1,
            Name = name,
            Slug = slug
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
    }

    private UpdateCommunityProfileCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new UpdateCommunityProfileCommandHandler(
            _userServiceMock.Object,
            _communityAccessServiceMock.Object,
            new BaseRepository<Community>(context));
    }

    private UpdateCommunityProfileCommandHandler CreateHandlerWithRealAccessService(
        ApplicationDbContext context)
    {
        return new UpdateCommunityProfileCommandHandler(
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
                Email = "owner@example.com",
                Name = "Owner",
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

    private static async Task SeedCommunities(
        ApplicationDbContext context,
        CommunityUserRole? role = null,
        CommunityUserStatus status = CommunityUserStatus.Active)
    {
        context.Communities.AddRange(
            new Community
            {
                Id = 1,
                Name = "Example School",
                Slug = "example-school",
                Status = CommunityStatus.Active
            },
            new Community
            {
                Id = 2,
                Name = "Other School",
                Slug = "other-school",
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
