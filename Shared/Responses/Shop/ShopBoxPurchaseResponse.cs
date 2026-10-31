namespace Shared.Responses.Shop;

public class ShopBoxPurchaseResponse
{
    public Guid RequestId { get; set; }
    public long ProductId { get; set; }
    public int CoinsSpent { get; set; }
    public int CoinsAfter { get; set; }
    public int QuantityOwned { get; set; }
    public bool IsReplay { get; set; }
}
