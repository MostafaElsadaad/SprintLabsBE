using MediatR;

namespace Application.Features.CommunityDashboard.ListDashboardNotifications;

public class ListDashboardNotificationsQuery : IRequest<NotificationsResponse>
{
    public long UserId { get; set; }
}
