namespace Shared.Responses;
public class PlayerMissionResponse
{
    public long PlayerMissionId { get; set; }
    public string MissionKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string PeriodType { get; set; } = "";
    public int CurrentProgress { get; set; }
    public int TargetProgress { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public List<MissionRewardResponse> Rewards { get; set; } = new();
}
