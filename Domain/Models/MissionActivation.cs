using Domain.Enums;
namespace Domain.Models;
public class MissionActivation
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public MissionPeriodType PeriodType { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int RandomMissionCount { get; set; }
    public bool AutoClaimCompletedOnReset { get; set; } = true;
    public MissionActivationStatus Status { get; set; } = MissionActivationStatus.Active;
    public DateTime CreatedAt { get; set; }
    public ICollection<MissionActivationItem> Items { get; set; } = new List<MissionActivationItem>();
}
