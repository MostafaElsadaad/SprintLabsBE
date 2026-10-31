using Domain.Enums;
namespace Domain.Models;
public class ShopProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? ItemId { get; set; }
    public int? CoinPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BoxKey { get; set; }
    public string? IconKey { get; set; }
    public int? DisplayLevel { get; set; }
    public string? CategoryOddsJson { get; set; }
    public ICollection<BoxRewardEntry> BoxRewards { get; set; } = new List<BoxRewardEntry>();
}
