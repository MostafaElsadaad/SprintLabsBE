namespace Shared.Responses.Shop;

public class ShopProductResponse
{
    public long ProductId { get; set; }
    public int CoinPrice { get; set; }
    public bool IsActive { get; set; }
    public ShopItemResponse Item { get; set; } = new();
}
