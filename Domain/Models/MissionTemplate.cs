using Domain.Enums;
namespace Domain.Models;
public class MissionTemplate
{
    public long Id { get; set; }
    public string MissionKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public MissionCategory Category { get; set; }
    public MissionPeriodType PeriodType { get; set; }
    public MissionEventType EventType { get; set; }
    public MissionProgressType ProgressType { get; set; }
    public int TargetValue { get; set; }
    public string ConditionsJson { get; set; } = "{}";
    public string ProgressField { get; set; } = "value";
    public MissionEventType? ResetEventType { get; set; }
    public bool MatchScoped { get; set; }
    public bool RepeatValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<MissionReward> Rewards { get; set; } = new List<MissionReward>();
}
