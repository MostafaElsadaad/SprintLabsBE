namespace Domain.Models;

public class ShopPurchase
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long ShopProductId { get; set; }
    public long ItemId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public int CoinsSpent { get; set; }
    public int CoinsAfter { get; set; }
    public DateTime PurchasedAt { get; set; }
}
