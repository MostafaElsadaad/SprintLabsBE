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
    public static TheoryData<CommunityUserRole, CommunityUserRole, CommunityUserStatus>
        CrossCommunityStaffIssueConflicts => new()
        {
            { CommunityUserRole.Teacher, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Teacher, CommunityUserRole.Owner, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserRole.Owner, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Owner, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserRole.Owner, CommunityUserStatus.Pending },
            { CommunityUserRole.Owner, CommunityUserRole.Owner, CommunityUserStatus.Active }
        };

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
    public async Task IssueAsync_pending_reinvite_replaces_complete_class_list_without_consuming_another_seat()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var firstClass = await SeedClassAsync(context, 1, 7, "Class 7A");
        var replacementClass = await SeedClassAsync(context, 1, 8, "Class 8A");
        var service = CreateService(context);

        var first = await service.IssueAsync(
            10,
            1,
            "teacher@example.com",
            new long[] { firstClass.Id, replacementClass.Id },
            CancellationToken.None);
        var license = await context.CommunityLicenses.SingleAsync();
        license.MaxTeachers = license.UsedTeachers;
        await context.SaveChangesAsync();

        var second = await service.IssueAsync(
            10,
            1,
            "teacher@example.com",
            new long[] { replacementClass.Id },
            CancellationToken.None);

        second.UserId.Should().Be(first.UserId);
        second.Status.Should().Be(CommunityUserStatus.Pending.ToString());
        second.Classes.Should().ContainSingle().Which.ClassId.Should().Be(replacementClass.Id);
        (await context.TeacherClassAssignments.SingleAsync(x => x.TeacherUserId == first.UserId)).ClassId
            .Should().Be(replacementClass.Id);
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);
        var invitations = await context.TeacherInvitations.OrderBy(x => x.Id).ToListAsync();
        invitations.Should().HaveCount(2);
        invitations[0].RevokedAt.Should().NotBeNull();
        invitations[1].RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_with_classes_creates_pending_teacher_assignments_and_returns_them()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var firstClass = await SeedClassAsync(context, 1, 7, "Class 7A");
        var secondClass = await SeedClassAsync(context, 1, 8, "Class 8A");
        var service = CreateService(context);

        var result = await service.IssueAsync(
            10,
            1,
            "teacher@example.com",
            new long[] { secondClass.Id, firstClass.Id, firstClass.Id },
            CancellationToken.None);

        (await context.CommunityUsers.SingleAsync(x => x.UserId == result.UserId)).Status
            .Should().Be(CommunityUserStatus.Pending);
        var assignments = await context.TeacherClassAssignments
            .Where(x => x.TeacherUserId == result.UserId)
            .OrderBy(x => x.ClassId)
            .ToListAsync();
        assignments.Select(x => x.ClassId).Should().Equal(firstClass.Id, secondClass.Id);
        result.Classes.Select(x => x.ClassId).Should().Equal(firstClass.Id, secondClass.Id);
    }

    [Fact]
    public async Task IssueAsync_with_foreign_class_rejects_without_creating_invitation_or_teacher()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context, communityId: 1, ownerId: 10);
        await SeedOwnerAndCommunityAsync(context, communityId: 2, ownerId: 11);
        var foreignClass = await SeedClassAsync(context, 2, 7, "Class 7A");
        var service = CreateService(context);

        var action = async () => await service.IssueAsync(
            10,
            1,
            "teacher@example.com",
            new long[] { foreignClass.Id },
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await context.Users.AnyAsync(x => x.NormalizedEmail == "TEACHER@EXAMPLE.COM")).Should().BeFalse();
        (await context.CommunityUsers.AnyAsync(x => x.Role == CommunityUserRole.Teacher)).Should().BeFalse();
        (await context.TeacherInvitations.AnyAsync()).Should().BeFalse();
        (await context.TeacherClassAssignments.AnyAsync()).Should().BeFalse();
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
    public async Task IssueAsync_removed_staff_membership_in_another_community_is_reassignable()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        await SeedCurrentStaffMembershipAsync(context, 2, 20, CommunityUserRole.Owner, CommunityUserStatus.Removed);
        var service = CreateService(context);

        var result = await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);

        result.CommunityId.Should().Be(1);
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == 20)).Status
            .Should().Be(CommunityUserStatus.Pending);
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 2 && x.UserId == 20)).Status
            .Should().Be(CommunityUserStatus.Removed);
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(CrossCommunityStaffIssueConflicts))]
    public async Task IssueAsync_rejects_every_other_community_current_staff_role_without_partial_mutation(
        CommunityUserRole issuedRole,
        CommunityUserRole existingRole,
        CommunityUserStatus existingStatus)
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        await SeedCurrentStaffMembershipAsync(context, 2, 20, existingRole, existingStatus);
        if (issuedRole == CommunityUserRole.Owner)
        {
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
            await context.SaveChangesAsync();
        }

        var service = CreateService(context);
        Func<Task> action = issuedRole == CommunityUserRole.Teacher
            ? () => service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None)
            : () => service.IssueCommunityAdminSetupAsync(99, 1, "teacher@example.com", CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await context.CommunityUsers.AnyAsync(x => x.CommunityId == 1 && x.UserId == 20)).Should().BeFalse();
        (await context.TeacherInvitations.AnyAsync()).Should().BeFalse();
        (await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(0);
        var target = await context.Users.SingleAsync(x => x.Id == 20);
        target.IsTeacherAccount.Should().BeFalse();
        target.UpdatedAt.Should().BeNull();
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

    [Theory]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Active)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Pending)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Active)]
    public async Task CompleteAsync_rejects_cross_community_current_staff_conflict_without_side_effects(
        CommunityUserRole conflictingRole,
        CommunityUserStatus conflictingStatus)
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var refreshTokens = new Mock<IRefreshTokenService>();
        var service = CreateService(context, refreshTokens.Object);
        var issue = await service.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);
        await SeedCurrentStaffMembershipAsync(context, 2, issue.UserId, conflictingRole, conflictingStatus, createUser: false);

        var action = async () => await service.CompleteAsync(
            issue.InvitationToken!,
            "Changed Name",
            "StrongPassword123!",
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        exception.Which.ErrorCode.Should().Be(ErrorCode.TeacherAlreadyBelongsToAnotherCommunity);
        var target = await context.Users.SingleAsync(x => x.Id == issue.UserId);
        target.Name.Should().Be("Invited Teacher");
        target.EmailConfirmed.Should().BeFalse();
        (await CreateUserManager(context).HasPasswordAsync(target)).Should().BeFalse();
        (await context.CommunityUsers.SingleAsync(x => x.CommunityId == 1 && x.UserId == issue.UserId)).Status
            .Should().Be(CommunityUserStatus.Pending);
        (await context.TeacherInvitations.SingleAsync()).AcceptedAt.Should().BeNull();
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);
        refreshTokens.Verify(x => x.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private static async Task<Class> SeedClassAsync(
        ApplicationDbContext context,
        long communityId,
        int gradeValue,
        string className)
    {
        var grade = new Grade
        {
            CommunityId = communityId,
            Value = gradeValue,
            Name = $"Grade {gradeValue}",
            SortOrder = gradeValue
        };
        var communityClass = new Class
        {
            CommunityId = communityId,
            Grade = grade,
            Name = className,
            Status = ClassStatus.Active
        };
        context.Classes.Add(communityClass);
        await context.SaveChangesAsync();
        return communityClass;
    }

    private static TeacherInvitationService CreateService(
        ApplicationDbContext context,
        IRefreshTokenService? refreshTokenService = null)
    {
        var manager = CreateUserManager(context);
        return new TeacherInvitationService(
            context,
            manager,
            refreshTokenService ?? Mock.Of<IRefreshTokenService>(),
            Options.Create(new TeacherAuthenticationOptions { InvitationLifetimeDays = 7 }));
    }

    private static async Task SeedCurrentStaffMembershipAsync(
        ApplicationDbContext context,
        long communityId,
        long userId,
        CommunityUserRole role,
        CommunityUserStatus status,
        bool createUser = true)
    {
        if (createUser)
        {
            context.Users.Add(new User
            {
                Id = userId,
                UserName = "teacher@example.com",
                NormalizedUserName = "TEACHER@EXAMPLE.COM",
                Email = "teacher@example.com",
                NormalizedEmail = "TEACHER@EXAMPLE.COM",
                Name = "Existing Staff",
                SecurityStamp = Guid.NewGuid().ToString(),
                Status = UserStatus.Active
            });
        }

        context.Communities.Add(new Community
        {
            Id = communityId,
            Name = $"Community {communityId}",
            Slug = $"community-{communityId}",
            Status = CommunityStatus.Active
        });
        context.CommunityUsers.Add(new CommunityUser
        {
            CommunityId = communityId,
            UserId = userId,
            Role = role,
            Status = status
        });
        await context.SaveChangesAsync();
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
