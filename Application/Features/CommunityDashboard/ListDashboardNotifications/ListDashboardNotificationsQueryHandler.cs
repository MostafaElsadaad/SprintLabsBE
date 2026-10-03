using Domain.Models;
using Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardNotifications;

public class ListDashboardNotificationsQueryHandler : IRequestHandler<ListDashboardNotificationsQuery, NotificationsResponse>
{
    private readonly DashboardAuthorization _authorization;
    private readonly IBaseRepository<Notification> _notifications;
    public ListDashboardNotificationsQueryHandler(DashboardAuthorization authorization, IBaseRepository<Notification> notifications)
    { _authorization = authorization; _notifications = notifications; }

    public async Task<NotificationsResponse> Handle(ListDashboardNotificationsQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        var query = _notifications.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId && x.RecipientUserId == request.UserId);
        var unread = await query.CountAsync(x => !x.IsRead, ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new NotificationView { Id = x.Id, Title = x.Title, Body = x.Body, CreatedAt = x.CreatedAt, IsRead = x.IsRead }).ToListAsync(ct);
        return new NotificationsResponse { Notifications = items, UnreadCount = unread };
    }
}
