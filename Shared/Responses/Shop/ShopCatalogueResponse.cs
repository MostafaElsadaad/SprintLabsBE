namespace Shared.Responses.Shop;

public class ShopCatalogueResponse
{
    public int Coins { get; set; }
    public List<ShopProductResponse> Products { get; set; } = new();
}
