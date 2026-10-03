namespace Shared.Responses;
public class MissionItemResponse
{
    public long ItemId { get; set; }
    public string ItemKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Rarity { get; set; } = "";
    public int Quantity { get; set; }
    public int QuantityOwned { get; set; }
    public int Weight { get; set; }
    public string IconKey { get; set; } = "";
    public string PrefabKey { get; set; } = "";
}
