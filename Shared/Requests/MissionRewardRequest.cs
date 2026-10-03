namespace Shared.Requests;
public class MissionRewardRequest
{
    public string RewardType { get; set; } = "";
    public int? Amount { get; set; }
    public long? ShopProductId { get; set; }
}
