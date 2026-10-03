namespace Shared.Requests;
public class MissionActivationRequest
{
    public string Name { get; set; } = "";
    public string PeriodType { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int RandomMissionCount { get; set; }
    public bool AutoClaimCompletedOnReset { get; set; } = true;
    public List<MissionActivationItemRequest> Items { get; set; } = new();
}
