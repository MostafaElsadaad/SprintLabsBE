using Domain.Enums;
namespace Domain.Models;
public class Item
{
    public long Id { get; set; }
    public string ItemKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Rarity { get; set; } = "Common";
    public string IconKey { get; set; } = "";
    public string PrefabKey { get; set; } = "";
}
