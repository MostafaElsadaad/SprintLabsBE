using System.Text.Json;
namespace Shared.Requests;
public class MissionEventRequest
{
    public string EventId { get; set; } = "";
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public long? MatchId { get; set; }
    public string EventType { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public Dictionary<string, JsonElement> EventData { get; set; } = new();
}
