using System.Net;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class TeacherInvitationServiceTests
{
    [Fact]
    public async Task IssueAsync_same_community_reissue_reuses_the_pending_membership_and_revokes_the_prior_token()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var service = CreateService(context);

        var first = await service.IssueAsync(10, 1, "Teacher@Example.com", CancellationToken.None);
        var second = await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);

        first.InvitationToken.Should().NotBeNull();
        second.InvitationToken.Should().NotBeNull().And.NotBe(first.InvitationToken);
        (await context.CommunityUsers.Where(x => x.UserId == first.UserId && x.Role == CommunityUserRole.Teacher).CountAsync()).Should().Be(1);
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);
        var invitations = await context.TeacherInvitations.OrderBy(x => x.Id).ToListAsync();
        invitations.Should().HaveCount(2);
        invitations[0].RevokedAt.Should().NotBeNull();
        invitations[0].TokenHash.Should().NotBe(first.InvitationToken);
        invitations[1].TokenHash.Should().NotBe(second.InvitationToken);
    }

    [Fact]
    public async Task IssueAsync_other_community_current_teacher_relationship_returns_stable_conflict_without_mutation()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context, communityId: 1, ownerId: 10);
        await SeedOwnerAndCommunityAsync(context, communityId: 2, ownerId: 11);
        var service = CreateService(context);
        await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);

        var action = async () => await service.IssueAsync(11, 2, "teacher@example.com", CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        exception.Which.ErrorCode.Should().Be(ErrorCode.TeacherAlreadyBelongsToAnotherCommunity);
        (await context.CommunityUsers.Where(x => x.Role == CommunityUserRole.Teacher).Select(x => x.CommunityId).Distinct().ToListAsync())
            .Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_returns_only_safe_setup_context_without_activating_membership()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var service = CreateService(context);
        var issue = await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);

        var result = await service.ValidateAsync(issue.InvitationToken!, CancellationToken.None);

        result.CommunityName.Should().Be("Community 1");
        result.MaskedEmail.Should().Be("t***r@example.com");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        (await context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Pending);
    }

    [Fact]
    public async Task CompleteAsync_activates_the_existing_membership_confirms_email_and_is_one_time()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var service = CreateService(context);
        var issue = await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);

        await service.CompleteAsync(issue.InvitationToken!, " Teacher Name ", "StrongPassword123!", CancellationToken.None);

        var user = await context.Users.SingleAsync(x => x.Id == issue.UserId);
        user.Name.Should().Be("Teacher Name");
        user.EmailConfirmed.Should().BeTrue();
        (await CreateUserManager(context).HasPasswordAsync(user)).Should().BeTrue();
        (await context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Active);
        (await context.TeacherInvitations.SingleAsync()).AcceptedAt.Should().NotBeNull();
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);

        var replay = async () => await service.CompleteAsync(issue.InvitationToken!, "Teacher Name", "StrongPassword123!", CancellationToken.None);
        var exception = await replay.Should().ThrowAsync<GenericException>();
        exception.Which.ErrorCode.Should().Be(ErrorCode.InvalidTeacherInvitation);
    }

    [Fact]
    public async Task IssueCommunityAdminSetupAsync_creates_a_pending_owner_that_can_set_a_password()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = 99,
            UserName = "platform.admin",
            NormalizedUserName = "PLATFORM.ADMIN",
            Email = "platform.admin@example.com",
            NormalizedEmail = "PLATFORM.ADMIN@EXAMPLE.COM",
            Name = "Platform Admin",
            IsPlatformAdmin = true,
            Status = UserStatus.Active
        });
        context.Communities.Add(new Community { Id = 1, Name = "Community 1", Slug = "community-1", Status = CommunityStatus.Active });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var issue = await service.IssueCommunityAdminSetupAsync(99, 1, "owner@example.com", CancellationToken.None);

        (await context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Role.Should().Be(CommunityUserRole.Owner);
        (await context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Pending);

        await service.CompleteAsync(issue.InvitationToken!, "Community Owner", "StrongPassword123!", CancellationToken.None);

        var owner = await context.Users.SingleAsync(x => x.Id == issue.UserId);
        owner.IsTeacherAccount.Should().BeFalse();
        owner.EmailConfirmed.Should().BeTrue();
        (await CreateUserManager(context).HasPasswordAsync(owner)).Should().BeTrue();
        (await context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Active);
    }

    private static ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task SeedOwnerAndCommunityAsync(ApplicationDbContext context, long communityId = 1, long ownerId = 10)
    {
        context.Users.Add(new User
        {
            Id = ownerId,
            UserName = $"owner{ownerId}@example.com",
            NormalizedUserName = $"OWNER{ownerId}@EXAMPLE.COM",
            Email = $"owner{ownerId}@example.com",
            NormalizedEmail = $"OWNER{ownerId}@EXAMPLE.COM",
            Name = "Owner",
            EmailConfirmed = true
        });
        context.Communities.Add(new Community { Id = communityId, Name = $"Community {communityId}", Slug = $"community-{communityId}", Status = CommunityStatus.Active });
        context.CommunityUsers.Add(new CommunityUser { CommunityId = communityId, UserId = ownerId, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        context.CommunityLicenses.Add(new CommunityLicense { CommunityId = communityId, MaxTeachers = 5, UsedTeachers = 0 });
        await context.SaveChangesAsync();
    }

    private static TeacherInvitationService CreateService(ApplicationDbContext context)
    {
        var manager = CreateUserManager(context);
        return new TeacherInvitationService(
            context,
            manager,
            Mock.Of<IRefreshTokenService>(),
            Options.Create(new TeacherAuthenticationOptions { InvitationLifetimeDays = 7 }));
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context);
        return new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
    }
}
