using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.EndLiveGame;

public class EndLiveGameCommand : IRequest<LiveGameResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long LiveGameId { get; set; }
}
