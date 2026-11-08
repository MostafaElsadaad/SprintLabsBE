using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Domain.Services;

/// <summary>
/// In-memory live view of running games (roster, scores, current question), folded from
/// game-server events. One API instance owns it; the database keeps the durable game row.
/// </summary>
public interface ILiveGameStateStore
{
    DateTime? GameServerLastSeenAt { get; }
    void MarkGameServerSeen();

    /// <summary>Applies one event. Returns false for a duplicate or unknown event.</summary>
    bool Apply(long liveGameId, LiveGameEventRequest liveEvent);

    int JoinedCount(long liveGameId);
    void Fill(long liveGameId, LiveGameResponse response);
    List<LiveGamePlayerResponse> Standings(long liveGameId);
}
