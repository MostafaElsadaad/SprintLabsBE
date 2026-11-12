using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.ListLiveGameSets;

/// <summary>Lists the caller's saved sets; with LiveGameSetId, returns that one set including its questions.</summary>
public class ListLiveGameSetsQuery : IRequest<List<LiveGameQuestionSetResponse>>
{
    public long AuthenticatedUserId { get; set; }
    public long? LiveGameSetId { get; set; }
}
