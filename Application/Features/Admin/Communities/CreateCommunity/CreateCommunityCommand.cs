using Application.Features.Admin.Communities.Common;

using MediatR;

namespace Application.Features.Admin.Communities.CreateCommunity;

public class CreateCommunityCommand : IRequest<CommunityResponse>
{
    public long AuthenticatedUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
