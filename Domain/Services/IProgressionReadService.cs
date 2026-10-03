using Shared.Requests;
using Shared.Responses;

namespace Domain.Services;

// Database/Identity boundary for eligibility joins and scoped game-history projections.
public interface IProgressionReadService
{
    Task<PlayerProgressionResponse> GetProgressionAsync(long userId, long? playerId, CancellationToken ct);
    Task<PlayerRankingResponse> GetRankingAsync(long userId, long? playerId, CancellationToken ct);
    Task<ProgressionPage<LeaderboardEntryResponse>> GetLeaderboardAsync(long userId, long? communityId, ProgressionPageRequest page, CancellationToken ct);
    Task<ProgressionPage<MatchHistoryResponse>> GetHistoryAsync(long userId, long? communityId, long? playerId, ProgressionPageRequest page, CancellationToken ct);
    Task<MatchDetailResponse> GetMatchAsync(long userId, long matchId, CancellationToken ct);
}
