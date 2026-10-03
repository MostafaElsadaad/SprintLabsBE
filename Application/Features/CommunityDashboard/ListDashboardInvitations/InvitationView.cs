namespace Application.Features.CommunityDashboard.ListDashboardInvitations;

public class InvitationView
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
