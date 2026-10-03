using Domain.Enums;
namespace Domain.Models;
public class BoxRewardEntry
{
    public long Id { get; set; }
    public long ShopProductId { get; set; }
    public long ItemId { get; set; }
    public int Weight { get; set; } = 1;
    public int Quantity { get; set; } = 1;
}
