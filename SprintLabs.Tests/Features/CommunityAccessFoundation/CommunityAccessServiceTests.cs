using Domain.Enums;
using Domain.Models;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace Compass.Tests.Features.CommunityAccessFoundation;

public class CommunityAccessServiceTests
{
    public static TheoryData<CommunityUserRole, CommunityUserStatus, CommunityUserRole, CommunityUserStatus>
        CrossCommunityStaffConflicts => new()
        {
            { CommunityUserRole.Owner, CommunityUserStatus.Active, CommunityUserRole.Owner, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserStatus.Active, CommunityUserRole.Owner, CommunityUserStatus.Pending },
            { CommunityUserRole.Owner, CommunityUserStatus.Pending, CommunityUserRole.Owner, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserStatus.Pending, CommunityUserRole.Owner, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserStatus.Active, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Teacher, CommunityUserStatus.Active, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserStatus.Pending, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Teacher, CommunityUserStatus.Pending, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Owner, CommunityUserStatus.Active, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserStatus.Active, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Owner, CommunityUserStatus.Pending, CommunityUserRole.Teacher, CommunityUserStatus.Active },
            { CommunityUserRole.Owner, CommunityUserStatus.Pending, CommunityUserRole.Teacher, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserStatus.Active, CommunityUserRole.Owner, CommunityUserStatus.Active },
            { CommunityUserRole.Teacher, CommunityUserStatus.Active, CommunityUserRole.Owner, CommunityUserStatus.Pending },
            { CommunityUserRole.Teacher, CommunityUserStatus.Pending, CommunityUserRole.Owner, CommunityUserStatus.Active },
            { CommunityUserRole.Teacher, CommunityUserStatus.Pending, CommunityUserRole.Owner, CommunityUserStatus.Pending }
        };

    [Fact]
    public async Task CanAccessCommunity_AllowsOnlyActiveMembership()
    {
        await using var context = CreateContext();
        await SeedMemberships(context);

        var service = CreateService(context);

        (await service.CanAccessCommunity(10, 1)).Should().BeTrue();
        (await service.CanAccessCommunity(10, 2)).Should().BeFalse();
        (await service.CanAccessCommunity(10, 3)).Should().BeFalse();
        (await service.CanAccessCommunity(10, 999)).Should().BeFalse();
    }

    [Fact]
    public async Task HasCommunityRole_RequiresActiveMembershipAndMatchingRole()
    {
        await using var context = CreateContext();
        await SeedMemberships(context);

        var service = CreateService(context);

        (await service.HasCommunityRole(10, 1, new[] { CommunityUserRole.Owner })).Should().BeTrue();
        (await service.HasCommunityRole(10, 1, new[] { CommunityUserRole.Teacher, CommunityUserRole.Owner })).Should().BeTrue();
        (await service.HasCommunityRole(10, 1, new[] { CommunityUserRole.Student })).Should().BeFalse();
        (await service.HasCommunityRole(10, 2, new[] { CommunityUserRole.Owner })).Should().BeFalse();
        (await service.HasCommunityRole(10, 1, Array.Empty<CommunityUserRole>())).Should().BeFalse();
        (await service.HasCommunityRole(10, 1, null)).Should().BeFalse();
    }

    [Theory]
    [InlineData(CommunityUserRole.Owner)]
    [InlineData(CommunityUserRole.Teacher)]
    public async Task ResolveCurrentStaffCommunityId_returns_one_active_staff_community(CommunityUserRole role)
    {
        await using var context = CreateContext();
        await AddMembershipAsync(context, 1, role, CommunityUserStatus.Active);

        var result = await CreateService(context).ResolveCurrentStaffCommunityId(10);

        result.Should().Be(1);
    }

    [Theory]
    [InlineData(null, null, CommunityStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Pending, CommunityStatus.Active)]
    [InlineData(CommunityUserRole.Teacher, CommunityUserStatus.Removed, CommunityStatus.Active)]
    [InlineData(CommunityUserRole.Student, CommunityUserStatus.Active, CommunityStatus.Active)]
    [InlineData(CommunityUserRole.Owner, CommunityUserStatus.Active, CommunityStatus.Suspended)]
    public async Task ResolveCurrentStaffCommunityId_fails_closed_without_one_eligible_active_staff_membership(
        CommunityUserRole? role,
        CommunityUserStatus? status,
        CommunityStatus communityStatus)
    {
        await using var context = CreateContext();
        if (role.HasValue && status.HasValue)
        {
            await AddMembershipAsync(context, 1, role.Value, status.Value, communityStatus);
        }

        var result = await CreateService(context).ResolveCurrentStaffCommunityId(10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveCurrentStaffCommunityId_ignores_removed_and_student_memberships()
    {
        await using var context = CreateContext();
        await AddMembershipAsync(context, 1, CommunityUserRole.Owner, CommunityUserStatus.Active);
        await AddMembershipAsync(context, 2, CommunityUserRole.Teacher, CommunityUserStatus.Removed);
        await AddMembershipAsync(context, 3, CommunityUserRole.Student, CommunityUserStatus.Active);

        var result = await CreateService(context).ResolveCurrentStaffCommunityId(10);

        result.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(CrossCommunityStaffConflicts))]
    public async Task ResolveCurrentStaffCommunityId_fails_closed_for_every_cross_community_staff_conflict(
        CommunityUserRole firstRole,
        CommunityUserStatus firstStatus,
        CommunityUserRole secondRole,
        CommunityUserStatus secondStatus)
    {
        await using var context = CreateContext();
        await AddMembershipAsync(context, 1, firstRole, firstStatus);
        await AddMembershipAsync(context, 2, secondRole, secondStatus);

        var result = await CreateService(context).ResolveCurrentStaffCommunityId(10);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveCurrentStaffCommunityId_never_selects_by_insertion_order(bool reverseOrder)
    {
        await using var context = CreateContext();
        var memberships = new[]
        {
            (CommunityId: 1L, Role: CommunityUserRole.Owner),
            (CommunityId: 2L, Role: CommunityUserRole.Teacher)
        };

        foreach (var membership in reverseOrder ? memberships.Reverse() : memberships)
        {
            await AddMembershipAsync(context, membership.CommunityId, membership.Role, CommunityUserStatus.Active);
        }

        (await CreateService(context).ResolveCurrentStaffCommunityId(10)).Should().BeNull();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CommunityAccessService CreateService(ApplicationDbContext context)
    {
        return new CommunityAccessService(new BaseRepository<CommunityUser>(context));
    }

    private static async Task SeedMemberships(ApplicationDbContext context)
    {
        context.Communities.AddRange(
            new Community { Id = 1, Name = "Active School", Slug = "active-school" },
            new Community { Id = 2, Name = "Pending School", Slug = "pending-school" },
            new Community { Id = 3, Name = "Removed School", Slug = "removed-school" });

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
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Pending
            },
            new CommunityUser
            {
                CommunityId = 3,
                UserId = 10,
                Role = CommunityUserRole.Owner,
                Status = CommunityUserStatus.Removed
            });

        await context.SaveChangesAsync();
    }

    private static async Task AddMembershipAsync(
        ApplicationDbContext context,
        long communityId,
        CommunityUserRole role,
        CommunityUserStatus status,
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
            UserId = 10,
            Role = role,
            Status = status
        });
        await context.SaveChangesAsync();
    }
}
