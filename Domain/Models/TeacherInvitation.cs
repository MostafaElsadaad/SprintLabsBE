namespace Domain.Models;

public class TeacherInvitation
{
    public long Id { get; set; }
    public long CommunityUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedByUserId { get; set; }
    public DateTime? LastSentAt { get; set; }
    public CommunityUser CommunityUser { get; set; } = default!;
}
