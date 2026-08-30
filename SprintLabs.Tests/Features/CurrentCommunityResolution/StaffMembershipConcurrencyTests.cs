using Compass.Tests.Fixtures;

using Domain.Enums;
using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.Services;
using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Options;

namespace Compass.Tests.Features.CurrentCommunityResolution;

public class StaffMembershipConcurrencyTests
{
    [MysqlFact]
    public async Task Invitation_and_owner_assignment_cannot_create_current_staff_memberships_in_two_communities()
    {
        var fixture = CreateFixtureOrSkip();
        await using (var seed = fixture.CreateContext())
        {
            await SeedAsync(seed, includeOwnerAndLicense: true);
        }

        await using var invitationContext = fixture.CreateContext();
        await using var ownerContext = fixture.CreateContext();
        var invitation = CreateInvitationService(invitationContext);
        var ownerAssignment = new StaffCommunityMembershipService(ownerContext);

        await Task.WhenAll(
            IgnoreExpectedConflict(() => invitation.IssueAsync(10, 1, "staff@example.com", CancellationToken.None)),
            IgnoreExpectedConflict(() => ownerAssignment.AssignOwnerAsync(20, 2, CancellationToken.None)));

        await using var verify = fixture.CreateContext();
        var communities = verify.CommunityUsers
            .Where(x => x.UserId == 20 &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                        (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
            .Select(x => x.CommunityId)
            .Distinct()
            .ToList();

        communities.Should().HaveCountLessOrEqualTo(1);
    }

    [MysqlFact]
    public async Task Cross_community_owner_assignments_cannot_create_current_staff_memberships_in_two_communities()
    {
        var fixture = CreateFixtureOrSkip();
        await using (var seed = fixture.CreateContext())
        {
            await SeedAsync(seed, includeOwnerAndLicense: false);
        }

        await using var firstContext = fixture.CreateContext();
        await using var secondContext = fixture.CreateContext();
        var first = new StaffCommunityMembershipService(firstContext);
        var second = new StaffCommunityMembershipService(secondContext);

        await Task.WhenAll(
            IgnoreExpectedConflict(() => first.AssignOwnerAsync(20, 1, CancellationToken.None)),
            IgnoreExpectedConflict(() => second.AssignOwnerAsync(20, 2, CancellationToken.None)));

        await using var verify = fixture.CreateContext();
        var communities = verify.CommunityUsers
            .Where(x => x.UserId == 20 &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                        (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
            .Select(x => x.CommunityId)
            .Distinct()
            .ToList();

        communities.Should().HaveCountLessOrEqualTo(1);
    }

    private static MysqlDatabaseFixture CreateFixtureOrSkip() => new();

    private static async Task SeedAsync(Infrastructure.DataAccess.ApplicationDbContext context, bool includeOwnerAndLicense)
    {
        context.Users.AddRange(
            new User
            {
                Id = 10,
                UserName = "owner@example.com",
                NormalizedUserName = "OWNER@EXAMPLE.COM",
                Email = "owner@example.com",
                NormalizedEmail = "OWNER@EXAMPLE.COM",
                Name = "Owner",
                Status = UserStatus.Active,
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new User
            {
                Id = 20,
                UserName = "staff@example.com",
                NormalizedUserName = "STAFF@EXAMPLE.COM",
                Email = "staff@example.com",
                NormalizedEmail = "STAFF@EXAMPLE.COM",
                Name = "Staff",
                Status = UserStatus.Active,
                SecurityStamp = Guid.NewGuid().ToString()
            });
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Community 1", Slug = "community-1", Status = CommunityStatus.Active },
            new Community { Id = 2, Name = "Community 2", Slug = "community-2", Status = CommunityStatus.Active });
        if (includeOwnerAndLicense)
        {
            context.CommunityUsers.Add(new CommunityUser
            {
                CommunityId = 1,
                UserId = 10,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Active
            });
            context.CommunityLicenses.Add(new CommunityLicense { CommunityId = 1, MaxTeachers = 5, UsedTeachers = 0 });
        }

        await context.SaveChangesAsync();
    }

    private static TeacherInvitationService CreateInvitationService(Infrastructure.DataAccess.ApplicationDbContext context)
    {
        var store = new UserStore<User, IdentityRole<long>, Infrastructure.DataAccess.ApplicationDbContext, long>(context);
        var manager = new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
        return new TeacherInvitationService(
            context,
            manager,
            Mock.Of<IRefreshTokenService>(),
            Options.Create(new TeacherAuthenticationOptions { InvitationLifetimeDays = 7 }));
    }

    private static async Task IgnoreExpectedConflict(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Shared.Exceptions.GenericException)
        {
        }
    }
}
