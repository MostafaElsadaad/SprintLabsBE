using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CommunityAccessService : ICommunityAccessService
{
    private readonly IBaseRepository<CommunityUser> _communityUserRepository;

    public CommunityAccessService(IBaseRepository<CommunityUser> communityUserRepository)
    {
        _communityUserRepository = communityUserRepository;
    }

    public Task<bool> CanAccessCommunity(long userId, long communityId)
    {
        return _communityUserRepository.AsQueryable()
            .AnyAsync(x =>
                x.UserId == userId
                && x.CommunityId == communityId
                && x.Status == CommunityUserStatus.Active);
    }

    public Task<bool> HasCommunityRole(long userId, long communityId, IEnumerable<CommunityUserRole>? roles)
    {
        var requiredRoles = roles?.ToList() ?? new List<CommunityUserRole>();
        if (requiredRoles.Count == 0)
        {
            return Task.FromResult(false);
        }

        return _communityUserRepository.AsQueryable()
            .AnyAsync(x =>
                x.UserId == userId
                && x.CommunityId == communityId
                && x.Status == CommunityUserStatus.Active
                && requiredRoles.Contains(x.Role));
    }

    public async Task<long?> ResolveCurrentStaffCommunityId(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var currentMemberships = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == userId &&
                        (x.Role == CommunityUserRole.Owner || x.Role == CommunityUserRole.Teacher) &&
                        (x.Status == CommunityUserStatus.Pending || x.Status == CommunityUserStatus.Active))
            .Select(x => new
            {
                x.CommunityId,
                MembershipStatus = x.Status,
                CommunityStatus = x.Community.Status
            })
            .ToListAsync(cancellationToken);

        var currentCommunityIds = currentMemberships
            .Select(x => x.CommunityId)
            .Distinct()
            .ToList();
        if (currentCommunityIds.Count != 1)
        {
            return null;
        }

        var communityId = currentCommunityIds[0];
        return currentMemberships.Any(x =>
            x.CommunityId == communityId &&
            x.MembershipStatus == CommunityUserStatus.Active &&
            x.CommunityStatus == CommunityStatus.Active)
                ? communityId
                : null;
    }
}
