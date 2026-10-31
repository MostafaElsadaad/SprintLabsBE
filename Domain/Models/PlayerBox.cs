namespace Domain.Models;

public class PlayerBox
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long ShopProductId { get; set; }
    public int Quantity { get; set; }
}
