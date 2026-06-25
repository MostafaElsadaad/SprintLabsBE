using Application.Features.Communities.Common;

using MediatR;

namespace Application.Features.Communities.UpdateCommunityProfile;

public class UpdateCommunityProfileCommand : IRequest<CommunityProfileResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
