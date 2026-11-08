using MediatR;
using Shared.Requests.LiveGames;

namespace Application.Features.LiveGames.RecordLiveGameEvents;

/// <summary>Returns how many events were new; duplicates (same EventId) are ignored.</summary>
public class RecordLiveGameEventsCommand : IRequest<int>
{
    public string Code { get; set; } = "";
    public List<LiveGameEventRequest> Events { get; set; } = new();
}
