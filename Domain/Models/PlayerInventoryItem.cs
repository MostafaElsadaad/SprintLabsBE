using Domain.Enums;
namespace Domain.Models;
public class PlayerInventoryItem
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long ItemId { get; set; }
    public int Quantity { get; set; }
}
