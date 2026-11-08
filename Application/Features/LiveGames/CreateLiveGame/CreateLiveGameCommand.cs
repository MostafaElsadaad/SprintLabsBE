using MediatR;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.CreateLiveGame;

public class CreateLiveGameCommand : IRequest<LiveGameResponse>
{
    public long AuthenticatedUserId { get; set; }
    public CreateLiveGameRequest Request { get; set; } = new();
}
