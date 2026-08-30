using Domain.Enums;
using Domain.Models;

using Infrastructure.DataAccess;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Common;

internal static class StaffCommunityMembershipIntegrity
{
    internal static async Task LockUserAsync(
        ApplicationDbContext context,
        long userId,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsRelational())
        {
            await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            return;
        }

        await context.Users
            .FromSqlInterpolated($"SELECT * FROM Users WHERE Id = {userId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static Task<bool> HasCurrentStaffMembershipInAnotherCommunityAsync(
        ApplicationDbContext context,
        long userId,
        long targetCommunityId,
        CancellationToken cancellationToken)
    {
        return context.CommunityUsers.AnyAsync(
            x => x.UserId == userId &&
                 x.CommunityId != targetCommunityId &&
                 (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                 (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active),
            cancellationToken);
    }

    internal static Task<List<CommunityUser>> GetCurrentStaffMembershipsAsync(
        ApplicationDbContext context,
        long userId,
        CancellationToken cancellationToken)
    {
        return context.CommunityUsers
            .Where(x => x.UserId == userId &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                        (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
