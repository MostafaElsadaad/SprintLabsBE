using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.StartLiveGame;

public class StartLiveGameCommand : IRequest<LiveGameResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long LiveGameId { get; set; }
}
