namespace Domain.Models;

public class ShopOperation
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public string RequestId { get; set; } = "";
    public string Action { get; set; } = "";
    public long TargetId { get; set; }
    public string ResultJson { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
