using Domain.Enums;
namespace Domain.Models;
public class MissionEventLog
{
    public long Id { get; set; }
    public string EventId { get; set; } = "";
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public long? MatchId { get; set; }
    public MissionEventType EventType { get; set; }
    public string EventDataJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ResultJson { get; set; } = "{}";
}
