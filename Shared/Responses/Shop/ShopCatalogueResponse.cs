namespace Shared.Responses.Shop;

public class ShopCatalogueResponse
{
    public List<ShopBoxResponse> Boxes { get; set; } = new();
    public int Coins { get; set; }
    public List<ShopProductResponse> Products { get; set; } = new();
}
