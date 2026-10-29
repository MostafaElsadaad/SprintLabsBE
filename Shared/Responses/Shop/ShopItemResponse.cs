namespace Shared.Responses.Shop;

public class ShopItemResponse
{
    public long ItemId { get; set; }
    public string ItemKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public string PrefabKey { get; set; } = string.Empty;
    public int QuantityOwned { get; set; }
    public bool IsCharacter { get; set; }
    public bool IsEquipped { get; set; }
}
