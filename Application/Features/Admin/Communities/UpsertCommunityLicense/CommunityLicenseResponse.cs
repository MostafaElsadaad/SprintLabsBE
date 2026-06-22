using Application.Features.Admin.Communities.Common;

namespace Application.Features.Admin.Communities.UpsertCommunityLicense;

public class CommunityLicenseResponse : CommunityLicenseSummaryResponse
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
}
