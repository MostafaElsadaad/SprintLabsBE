using Domain.Enums;
namespace Domain.Models;
public class MissionReward
{
    public long Id { get; set; }
    public long MissionTemplateId { get; set; }
    public MissionRewardType RewardType { get; set; }
    public int? Amount { get; set; }
    public long? ShopProductId { get; set; }
}
