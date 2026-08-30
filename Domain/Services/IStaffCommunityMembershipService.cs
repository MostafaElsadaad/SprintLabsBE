using Domain.Models;

namespace Domain.Services;

public interface IStaffCommunityMembershipService
{
    Task<CommunityUser> AssignOwnerAsync(
        long userId,
        long communityId,
        CancellationToken cancellationToken);
}
