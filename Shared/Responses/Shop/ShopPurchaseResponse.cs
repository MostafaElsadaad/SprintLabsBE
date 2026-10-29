namespace Shared.Responses.Shop;

public class ShopPurchaseResponse
{
    public long PurchaseId { get; set; }
    public long ProductId { get; set; }
    public long ItemId { get; set; }
    public Guid RequestId { get; set; }
    public int CoinsSpent { get; set; }
    public int CoinsAfter { get; set; }
    public DateTime PurchasedAt { get; set; }
    public bool IsReplay { get; set; }
}
