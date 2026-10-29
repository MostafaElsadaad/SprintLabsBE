namespace Shared.Requests.Shop;

public class EquipShopItemRequest
{
    // null restores the game's default character.
    [System.Text.Json.Serialization.JsonRequired]
    public long? ItemId { get; set; }
}
