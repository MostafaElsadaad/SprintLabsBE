namespace Application.Features.CommunityDashboard.StaffIdentity;

public class StaffIdentityResponse
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int UnreadNotificationsCount { get; set; }
}
