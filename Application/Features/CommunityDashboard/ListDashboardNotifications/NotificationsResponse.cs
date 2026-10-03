using Shared.Responses;

namespace Application.Features.CommunityDashboard.ListDashboardNotifications;

public class NotificationsResponse
{
    public List<NotificationView> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}
