using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.ListLiveGames;

public class ListLiveGamesQuery : IRequest<List<LiveGameResponse>>
{
    public long AuthenticatedUserId { get; set; }
}
