namespace Shared.Responses;
public class MissionRewardResponse
{
    public string RewardType { get; set; } = "";
    public int? Amount { get; set; }
    public long? ShopProductId { get; set; }
    public string? BoxName { get; set; }
    public MissionItemResponse? RewardItem { get; set; }
}
