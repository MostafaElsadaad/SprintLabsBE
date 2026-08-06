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

    public async Task<long?> GetSingleActiveCommunityIdForRole(long userId, CommunityUserRole role)
    {
        var communityIds = await _communityUserRepository.AsQueryable()
            .Where(x => x.UserId == userId &&
                        x.Role == role &&
                        x.Status == CommunityUserStatus.Active &&
                        x.Community.Status == CommunityStatus.Active)
            .Select(x => x.CommunityId)
            .Take(2)
            .ToListAsync();

        return communityIds.Count == 1 ? communityIds[0] : null;
    }
}
