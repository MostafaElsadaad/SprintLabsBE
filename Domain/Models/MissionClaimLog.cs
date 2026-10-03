using Domain.Enums;
namespace Domain.Models;
public class MissionClaimLog
{
    public long Id { get; set; }
    public long PlayerMissionId { get; set; }
    public long PlayerProfileId { get; set; }
    public long MissionTemplateId { get; set; }
    public MissionRewardType RewardType { get; set; }
    public int Amount { get; set; }
    public long? ShopProductId { get; set; }
    public long? RewardItemId { get; set; }
    public DateTime CreatedAt { get; set; }
}
