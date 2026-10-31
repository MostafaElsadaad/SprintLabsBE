namespace Shared.Responses.Shop;

public class ShopBoxResponse
{
    public long ProductId { get; set; }
    public string BoxKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconKey { get; set; } = "";
    public int DisplayLevel { get; set; }
    public int CoinPrice { get; set; }
    public int DrawCount { get; set; } = 5;
    public bool IsActive { get; set; }
    public int QuantityOwned { get; set; }
    public List<ShopCategoryOddsResponse> Odds { get; set; } = new();
}
