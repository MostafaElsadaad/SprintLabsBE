using MediatR;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.SaveLiveGameSet;

/// <summary>Saves a new set (LiveGameSetId null) or overwrites one the caller owns, keeping question order.</summary>
public class SaveLiveGameSetCommand : IRequest<LiveGameQuestionSetResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long? LiveGameSetId { get; set; }
    public LiveGameQuestionSetRequest Request { get; set; } = new();
}
