using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.AddLiveGameBot;

/// <summary>Asks the game server to add one more bot to a live game lobby (for testing with few players).</summary>
public class AddLiveGameBotCommand : IRequest<LiveGameResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long LiveGameId { get; set; }
}
