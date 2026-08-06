using Domain.Enums;

namespace Domain.Services;

public interface ICommunityAccessService
{
    Task<bool> CanAccessCommunity(long userId, long communityId);
    Task<bool> HasCommunityRole(long userId, long communityId, IEnumerable<CommunityUserRole>? roles);
    Task<long?> GetSingleActiveCommunityIdForRole(long userId, CommunityUserRole role);
}
