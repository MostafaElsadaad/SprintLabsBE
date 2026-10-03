using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.StaffIdentity;

public class StaffIdentityQueryHandler : IRequestHandler<StaffIdentityQuery, StaffIdentityResponse>
{
    private readonly DashboardAuthorization _authorization;
    private readonly IUserService _users;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<CommunityUser> _members;
    private readonly IBaseRepository<Notification> _notifications;

    public StaffIdentityQueryHandler(DashboardAuthorization authorization, IUserService users, IBaseRepository<CommunityUser> members, IBaseRepository<Notification> notifications, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection; _users = users; _members = members; _notifications = notifications;
    }

    public async Task<StaffIdentityResponse> Handle(StaffIdentityQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        var user = await _users.GetCurrentUser(request.UserId);
        var member = await _members.AsQueryable().SingleAsync(x => x.CommunityId == scope.CommunityId &&
            x.UserId == request.UserId && x.Status == CommunityUserStatus.Active, ct);
        var grades = (await _projection.ClassesAsync(scope, ct)).Where(x => x.Status != "ARCHIVED")
            .Select(x => x.Grade).DistinctBy(x => x.Id).OrderBy(x => x.Value).Select(x => x.Name).ToList();
        return new StaffIdentityResponse { Id = request.UserId, CommunityId = scope.CommunityId, FullName = user!.Name,
            Role = scope.IsOwner ? "COMMUNITY_ADMIN" : "TEACHER", Title = scope.IsOwner || grades.Count == 0 ? null : string.Join(", ", grades),
            UnreadNotificationsCount = await _notifications.AsQueryable().CountAsync(x => x.CommunityId == scope.CommunityId &&
                x.RecipientUserId == request.UserId && !x.IsRead, ct) };
    }
}
