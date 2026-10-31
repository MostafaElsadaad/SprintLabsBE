namespace Shared.Responses.Shop;

public class ShopBoxOpenResponse
{
    public Guid RequestId { get; set; }
    public long ProductId { get; set; }
    public List<ShopItemResponse> Rewards { get; set; } = new();
    public int QuantityOwned { get; set; }
    public int CoinsAfter { get; set; }
    public bool IsReplay { get; set; }
}
