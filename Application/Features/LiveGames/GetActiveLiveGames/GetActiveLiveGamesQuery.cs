using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetActiveLiveGames;

/// <summary>Game-server poll: open lobbies, running games and recently ended games.</summary>
public class GetActiveLiveGamesQuery : IRequest<List<ActiveLiveGameResponse>>
{
}
