using Application.Features.Admin.Communities.Common;

namespace Application.Features.Admin.Communities.AssignOwner;

public class OwnerResponse : OwnerSummaryResponse
{
    public long CommunityUserId { get; set; }
    public long CommunityId { get; set; }
    public string Role { get; set; } = string.Empty;
}
