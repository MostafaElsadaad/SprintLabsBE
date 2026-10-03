using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.TeacherStats;

public class TeacherStatsQueryHandler : IRequestHandler<TeacherStatsQuery, TeacherStatsResponse>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<TeacherInvitation> _invitations;

    public TeacherStatsQueryHandler(DashboardAuthorization authorization, DashboardProjection projection, IBaseRepository<TeacherInvitation> invitations)
    {
        _authorization = authorization;
        _projection = projection;
        _invitations = invitations;
    }

    public async Task<TeacherStatsResponse> Handle(TeacherStatsQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        var rows = await _projection.TeachersAsync(scope, ct);
        var pending = await _invitations.AsQueryable().CountAsync(x => x.CommunityUser.CommunityId == scope.CommunityId &&
            x.CommunityUser.Role == CommunityUserRole.Teacher && x.CommunityUser.Status == CommunityUserStatus.Pending &&
            x.AcceptedAt == null && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        return new TeacherStatsResponse { Total = rows.Count,
            Active = rows.Count(x => x.Status == "ACTIVE"), Inactive = rows.Count(x => x.Status == "INACTIVE"), PendingInvitations = pending };
    }
}
