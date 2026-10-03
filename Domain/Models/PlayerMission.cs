using Domain.Enums;
namespace Domain.Models;
public class PlayerMission
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long MissionActivationId { get; set; }
    public long MissionTemplateId { get; set; }
    public int CurrentProgress { get; set; }
    public int TargetProgress { get; set; }
    public PlayerMissionStatus Status { get; set; } = PlayerMissionStatus.Assigned;
    public string ProgressStateJson { get; set; } = "{}";
    public string DefinitionSnapshotJson { get; set; } = "{}";
    public string? ClaimSnapshotJson { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastEventAt { get; set; }
}
