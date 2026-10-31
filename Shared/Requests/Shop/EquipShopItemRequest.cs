namespace Shared.Requests.Shop;

public class EquipShopItemRequest
{
    public string? Category { get; set; }
    // null restores the game's default character.
    [System.Text.Json.Serialization.JsonRequired]
    public long? ItemId { get; set; }
}
