using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Options;
using Shared.Enums;

namespace Compass.Tests.Features.CurrentCommunityResolution;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task RotateAsync_platform_administrator_with_conflicting_staff_memberships_does_not_revoke_or_replace()
    {
        await using var context = CreateContext();
        await SeedUserAndMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, 1, isPlatformAdmin: true);
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Pending, 2);
        var repository = RepositoryForCurrentToken();

        var result = await CreateService(context, repository.Object).RotateAsync("raw", null, CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(x => x.TryRevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateAsync_active_staff_membership_in_suspended_second_community_does_not_revoke_or_replace()
    {
        await using var context = CreateContext();
        await SeedUserAndMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Active, 2, CommunityStatus.Suspended);
        var repository = RepositoryForCurrentToken();

        var result = await CreateService(context, repository.Object).RotateAsync("raw", null, CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(x => x.TryRevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner, CommunityUserRole.Teacher)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserRole.Owner)]
    public async Task RotateAsync_current_memberships_in_two_communities_does_not_revoke_or_replace(
        CommunityUserRole firstRole,
        CommunityUserRole secondRole)
    {
        await using var context = CreateContext();
        await SeedUserAndMembershipAsync(context, firstRole, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, secondRole, CommunityUserStatus.Pending, 2);
        var repository = RepositoryForCurrentToken();

        var result = await CreateService(context, repository.Object).RotateAsync("raw", null, CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(x => x.TryRevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner, AuthenticatedAccountType.CommunityAdmin)]
    [InlineData(CommunityUserRole.Teacher, AuthenticatedAccountType.Teacher)]
    public async Task RotateAsync_one_valid_staff_membership_reconstructs_the_account_type(
        CommunityUserRole role,
        AuthenticatedAccountType expectedAccountType)
    {
        await using var context = CreateContext();
        await SeedUserAndMembershipAsync(context, role, CommunityUserStatus.Active, 1);
        var repository = RepositoryForCurrentToken();

        var result = await CreateService(context, repository.Object).RotateAsync("raw", null, CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccountType.Should().Be(expectedAccountType);
        repository.Verify(x => x.TryRevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_removed_staff_and_student_memberships_do_not_conflict_with_one_valid_teacher_membership()
    {
        await using var context = CreateContext();
        await SeedUserAndMembershipAsync(context, CommunityUserRole.Teacher, CommunityUserStatus.Active, 1);
        await SeedMembershipAsync(context, CommunityUserRole.Owner, CommunityUserStatus.Removed, 2);
        await SeedMembershipAsync(context, CommunityUserRole.Student, CommunityUserStatus.Active, 3);
        var repository = RepositoryForCurrentToken();

        var result = await CreateService(context, repository.Object).RotateAsync("raw", null, CancellationToken.None);

        result.Should().NotBeNull();
        result!.AccountType.Should().Be(AuthenticatedAccountType.Teacher);
        repository.Verify(x => x.TryRevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RefreshTokenService CreateService(ApplicationDbContext context, IRefreshTokenRepository repository) => new(
        repository,
        context,
        Options.Create(new JWTOptions { RefreshTokenLifetimeDays = 7 }));

    private static Mock<IRefreshTokenRepository> RepositoryForCurrentToken()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(x => x.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshToken
            {
                UserId = 20,
                TokenHash = "hash",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        repository.Setup(x => x.TryRevokeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return repository;
    }

    private static async Task SeedUserAndMembershipAsync(
        ApplicationDbContext context,
        CommunityUserRole role,
        CommunityUserStatus status,
        long communityId,
        bool isPlatformAdmin = false)
    {
        context.Users.Add(new User
        {
            Id = 20,
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "User",
            Status = UserStatus.Active,
            IsPlatformAdmin = isPlatformAdmin
        });
        await SeedMembershipAsync(context, role, status, communityId);
    }

    private static async Task SeedMembershipAsync(
        ApplicationDbContext context,
        CommunityUserRole role,
        CommunityUserStatus status,
        long communityId,
        CommunityStatus communityStatus = CommunityStatus.Active)
    {
        context.Communities.Add(new Community
        {
            Id = communityId,
            Name = $"Community {communityId}",
            Slug = $"community-{communityId}",
            Status = communityStatus
        });
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = communityId,
            UserId = 20,
            Role = role,
            Status = status
        });
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
