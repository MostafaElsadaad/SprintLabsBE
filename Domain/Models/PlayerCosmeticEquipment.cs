namespace Domain.Models;

public class PlayerCosmeticEquipment
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public string Category { get; set; } = "";
    public long ItemId { get; set; }
}
