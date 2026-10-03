namespace Domain.Models;

public class Notification
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public long RecipientUserId { get; set; }
    public long SenderUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
