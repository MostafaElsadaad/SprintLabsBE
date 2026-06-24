using MediatR;

namespace Application.Features.Users.GetCurrentUserCommunities;

public class GetCurrentUserCommunitiesQuery : IRequest<List<UserCommunityResponse>>
{
    public long UserId { get; set; }
}
