using Application.Features.Admin.Communities.Common;

namespace Application.Features.Admin.Communities.ListCommunities;

public class CommunityListItemResponse : CommunityResponse
{
    public OwnerSummaryResponse? Owner { get; set; }
    public CommunityLicenseSummaryResponse? License { get; set; }
}
