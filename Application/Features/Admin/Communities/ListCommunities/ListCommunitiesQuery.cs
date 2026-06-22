using MediatR;

namespace Application.Features.Admin.Communities.ListCommunities;

public class ListCommunitiesQuery : IRequest<List<CommunityListItemResponse>>
{
    public long AuthenticatedUserId { get; set; }
}
