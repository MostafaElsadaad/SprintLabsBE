namespace Shared.Requests.Shop;

public class ShopProductRequest
{
    public string Name { get; set; } = string.Empty;
    public int CoinPrice { get; set; }
    public string IconKey { get; set; } = string.Empty;
    public string PrefabKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
