namespace Shared.Responses.Shop;

public class ShopInventoryResponse
{
    public int Coins { get; set; }
    public long? EquippedCharacterItemId { get; set; }
    public List<ShopItemResponse> Items { get; set; } = new();
}
