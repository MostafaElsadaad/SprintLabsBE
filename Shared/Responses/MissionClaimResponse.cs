namespace Shared.Responses;
public class MissionClaimResponse
{
    public long PlayerMissionId { get; set; }
    public string Status { get; set; } = "";
    public List<MissionRewardResponse> Rewards { get; set; } = new();
    public int NewXp { get; set; }
    public int NewLevel { get; set; }
    public int NewCoins { get; set; }
}
