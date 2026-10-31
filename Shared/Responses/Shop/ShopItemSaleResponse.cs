namespace Shared.Responses.Shop;

public class ShopItemSaleResponse
{
    public Guid RequestId { get; set; }
    public long ItemId { get; set; }
    public int QuantitySold { get; set; } = 1;
    public int CoinsGained { get; set; }
    public int CoinsAfter { get; set; }
    public int QuantityOwned { get; set; }
    public bool IsReplay { get; set; }
}
