namespace Shared.Responses;
public class MissionEventResponse
{
    public string EventId { get; set; } = "";
    public bool Duplicate { get; set; }
    public List<PlayerMissionResponse> UpdatedMissions { get; set; } = new();
}
