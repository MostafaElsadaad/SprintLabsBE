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
}
