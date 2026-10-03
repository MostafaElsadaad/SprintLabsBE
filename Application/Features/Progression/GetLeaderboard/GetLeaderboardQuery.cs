using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetLeaderboard;

public class GetLeaderboardQuery : IRequest<ProgressionPage<LeaderboardEntryResponse>>
{
    public long UserId { get; set; }
    public long? CommunityId { get; set; }
    public ProgressionPageRequest Page { get; set; } = new();
}
