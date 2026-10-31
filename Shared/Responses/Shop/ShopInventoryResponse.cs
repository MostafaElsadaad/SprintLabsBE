namespace Shared.Responses.Shop;

public class ShopInventoryResponse
{
    public List<ShopBoxResponse> Boxes { get; set; } = new();
    public List<ShopEquippedCosmeticResponse> EquippedCosmetics { get; set; } = new();
    public int Coins { get; set; }
    public long? EquippedCharacterItemId { get; set; }
    public List<ShopItemResponse> Items { get; set; } = new();
}
