using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetLeaderboard;

public class GetLeaderboardQuery : IRequest<LeaderboardPageResponse>
{
    public long UserId { get; set; }
    public long? CommunityId { get; set; }
    public LeaderboardRequest Page { get; set; } = new();
}
