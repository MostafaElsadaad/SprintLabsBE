using Application.Features.Communities.Common;

using MediatR;

namespace Application.Features.Communities.GetCommunityProfile;

public class GetCommunityProfileQuery : IRequest<CommunityProfileResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
}
