using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetLeaderboardStanding;

public class GetLeaderboardStandingQuery : IRequest<LeaderboardStandingResponse>
{
    public long UserId { get; set; }
    public long? CommunityId { get; set; }
    public LeaderboardRequest Request { get; set; } = new();
}
