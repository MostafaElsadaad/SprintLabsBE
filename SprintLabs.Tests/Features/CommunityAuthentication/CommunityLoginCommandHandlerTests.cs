using Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.CommunityAuthentication;

public class CommunityLoginCommandHandlerTests
{
    [Theory]
    [InlineData(CommunityUserRole.Owner, "CommunityAdmin", AuthenticatedAccountType.CommunityAdmin)]
    [InlineData(CommunityUserRole.Teacher, "Teacher", AuthenticatedAccountType.Teacher)]
    public async Task Handle_active_supported_membership_returns_the_trusted_public_role(
        CommunityUserRole role,
        string publicRole,
        AuthenticatedAccountType accountType)
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, role, CommunityUserStatus.Active);
        var identity = Identity(20);
        var access = Access(20, accountType);
        var refresh = Refresh(20);

        var result = await CreateHandler(context, identity.Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher@example.com", Password = "password" }, CancellationToken.None);

        result.Role.Should().Be(publicRole);
        result.Community.Id.Should().Be(1);
        result.Community.Name.Should().Be("Community");
        result.Username.Should().Be("community.user");
    }

    [Fact]
    public async Task Handle_platform_administrator_returns_platform_admin_without_a_community()
    {
        await using var context = CreateContext();
        var identity = Identity(20, isPlatformAdmin: true);
        var access = Access(20, AuthenticatedAccountType.PlatformAdmin);
        var refresh = Refresh(20);

        var result = await CreateHandler(context, identity.Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "platform.admin", Password = "password" }, CancellationToken.None);

        result.Role.Should().Be("PlatformAdmin");
        result.Community.Should().BeNull();
        access.Verify(x => x.Create(20, "teacher@example.com", "User", AuthenticatedAccountType.PlatformAdmin), Times.Once);
        refresh.Verify(x => x.IssueAsync(20, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(CommunityUserStatus.Pending)]
    [InlineData(CommunityUserStatus.Removed)]
    public async Task Handle_non_active_membership_returns_generic_invalid_credentials(CommunityUserStatus status)
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, status);

        var action = async () => await CreateHandler(context, Identity(20).Object, new Mock<IAccessTokenService>().Object, new Mock<IRefreshTokenService>().Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        exception.Which.ErrorCode.Should().Be(ErrorCode.Failure);
    }

    [Fact]
    public async Task Handle_multiple_active_supported_memberships_returns_stable_conflict_without_tokens()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Active, 2);
        var access = new Mock<IAccessTokenService>();
        var refresh = new Mock<IRefreshTokenService>();

        var action = async () => await CreateHandler(context, Identity(20).Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        access.VerifyNoOtherCalls();
        refresh.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner, CommunityUserRole.Teacher)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserRole.Owner)]
    public async Task Handle_current_memberships_in_two_communities_returns_conflict_before_tokens(
        CommunityUserRole firstRole,
        CommunityUserRole secondRole)
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, firstRole, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, secondRole, CommunityUserStatus.Pending, 2);
        var access = new Mock<IAccessTokenService>();
        var refresh = new Mock<IRefreshTokenService>();

        var action = async () => await CreateHandler(context, Identity(20).Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        access.VerifyNoOtherCalls();
        refresh.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_platform_administrator_with_conflicting_staff_memberships_returns_conflict_before_tokens()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Pending, 2);
        var access = new Mock<IAccessTokenService>();
        var refresh = new Mock<IRefreshTokenService>();

        var action = async () => await CreateHandler(context, Identity(20, isPlatformAdmin: true).Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "platform.admin", Password = "password" }, CancellationToken.None);

        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        access.VerifyNoOtherCalls();
        refresh.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_active_staff_membership_in_suspended_second_community_returns_conflict_before_tokens()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Active, 2, CommunityStatus.Suspended);
        var access = new Mock<IAccessTokenService>();
        var refresh = new Mock<IRefreshTokenService>();

        var action = async () => await CreateHandler(context, Identity(20).Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        access.VerifyNoOtherCalls();
        refresh.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_removed_staff_and_student_memberships_do_not_conflict_with_one_valid_teacher_membership()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Removed, 2);
        await SeedMembershipAsync(context, CommunityUserRole.Student, CommunityUserStatus.Active, 3);
        var access = Access(20, AuthenticatedAccountType.Teacher);
        var refresh = Refresh(20);

        var result = await CreateHandler(context, Identity(20).Object, access.Object, refresh.Object)
            .Handle(new CommunityLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        result.Role.Should().Be("Teacher");
        result.Community!.Id.Should().Be(1);
        access.Verify(x => x.Create(20, "teacher@example.com", "User", AuthenticatedAccountType.Teacher), Times.Once);
        refresh.Verify(x => x.IssueAsync(20, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CommunityLoginCommandHandler CreateHandler(
        ApplicationDbContext context,
        ITeacherIdentityService identity,
        IAccessTokenService access,
        IRefreshTokenService refresh) => new(
        identity,
        access,
        refresh,
        new BaseRepository<CommunityUser>(context),
        NullLogger<CommunityLoginCommandHandler>.Instance);

    private static Mock<ITeacherIdentityService> Identity(long userId, bool isPlatformAdmin = false)
    {
        var identity = new Mock<ITeacherIdentityService>();
        identity.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherIdentityResult
            {
                UserId = userId,
                IsPlatformAdmin = isPlatformAdmin,
                Name = "User",
                Username = "community.user",
                Email = "teacher@example.com"
            });
        return identity;
    }

    private static Mock<IAccessTokenService> Access(long userId, AuthenticatedAccountType accountType)
    {
        var access = new Mock<IAccessTokenService>();
        access.Setup(x => x.Create(userId, "teacher@example.com", "User", accountType))
            .Returns(new AccessTokenResult { AccessToken = "access", AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(5) });
        return access;
    }

    private static Mock<IRefreshTokenService> Refresh(long userId)
    {
        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(x => x.IssueAsync(userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult { RefreshToken = "refresh", RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1) });
        return refresh;
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task SeedMembershipAsync(
        ApplicationDbContext context,
        CommunityUserRole role,
        CommunityUserStatus status,
        long communityId = 1,
        CommunityStatus communityStatus = CommunityStatus.Active)
    {
        context.Communities.Add(new Community { Id = communityId, Name = "Community", Slug = $"community-{communityId}", Status = communityStatus });
        context.CommunityUsers.Add(new CommunityUser { CommunityId = communityId, UserId = 20, Role = role, Status = status });
        await context.SaveChangesAsync();
    }
}
