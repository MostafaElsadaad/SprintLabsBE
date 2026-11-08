using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetLiveGame;

public class GetLiveGameQuery : IRequest<LiveGameResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long LiveGameId { get; set; }
}
