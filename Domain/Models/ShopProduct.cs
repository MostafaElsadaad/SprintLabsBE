using Domain.Enums;
namespace Domain.Models;
public class ShopProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<BoxRewardEntry> BoxRewards { get; set; } = new List<BoxRewardEntry>();
}
