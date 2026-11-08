using Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Shared.Responses.LiveGames;

namespace API.Hubs;

public sealed class SignalRLiveGameBroadcaster : ILiveGameBroadcaster
{
    private readonly IHubContext<LiveGameHub> _hub;
    private readonly ILogger<SignalRLiveGameBroadcaster> _logger;

    public SignalRLiveGameBroadcaster(IHubContext<LiveGameHub> hub, ILogger<SignalRLiveGameBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishAsync(LiveGameResponse snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.Group(LiveGameHub.GroupName(snapshot.Id))
                .SendAsync(LiveGameHub.UpdatedMethod, snapshot, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A dashboard push must never fail a game-server event batch; clients also poll.
            _logger.LogWarning(exception, "Live game {LiveGameId} update could not be pushed.", snapshot.Id);
        }
    }
}
