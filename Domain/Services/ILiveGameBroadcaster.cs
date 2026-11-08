using Shared.Responses.LiveGames;

namespace Domain.Services;

/// <summary>Pushes a live game snapshot to dashboards that watch it (SignalR in the API).</summary>
public interface ILiveGameBroadcaster
{
    Task PublishAsync(LiveGameResponse snapshot, CancellationToken cancellationToken);
}
