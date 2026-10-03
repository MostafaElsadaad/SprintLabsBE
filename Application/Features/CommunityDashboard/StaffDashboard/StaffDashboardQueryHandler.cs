using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.StaffDashboard;

public class StaffDashboardQueryHandler : IRequestHandler<StaffDashboardQuery, StaffDashboardResponse>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<TeacherInvitation> _invitations;
    private readonly IBaseRepository<StudentLicense> _licenses;

    public StaffDashboardQueryHandler(DashboardAuthorization authorization, DashboardProjection projection, IBaseRepository<TeacherInvitation> invitations, IBaseRepository<StudentLicense> licenses)
    {
        _authorization = authorization;
        _projection = projection;
        _invitations = invitations;
        _licenses = licenses;
    }

    public async Task<StaffDashboardResponse> Handle(StaffDashboardQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        DashboardPeriod.Range(request.From, request.To);
        var classes = (await _projection.ClassesAsync(scope, ct)).Where(x => x.Status != "ARCHIVED").ToList();
        var classIds = classes.Select(x => x.Id).ToList();
        var week = DashboardPeriod.WeekStart(DateTime.UtcNow);
        var licenses = _licenses.AsQueryable().Where(x => x.CommunityId == scope.CommunityId &&
            classIds.Contains(x.ClassId) && x.Status != StudentLicenseStatus.Revoked);
        return new StaffDashboardResponse
        {
            Role = scope.IsOwner ? "COMMUNITY_ADMIN" : "TEACHER",
            Classes = classes, ActiveClasses = classes.Count, TotalStudents = await licenses.CountAsync(ct),
            NewStudentsThisWeek = await licenses.CountAsync(x => x.CreatedAt >= week && x.CreatedAt < week.AddDays(7), ct),
            GradesCount = scope.IsOwner ? 6 : classes.Select(x => x.Grade.Id).Distinct().Count(),
            TeachersCount = scope.IsOwner ? (await _projection.TeachersAsync(scope, ct)).Count(x => x.Status != "INACTIVE") : null,
            PendingInvitationsCount = scope.IsOwner ? await _invitations.AsQueryable().CountAsync(x =>
                x.CommunityUser.CommunityId == scope.CommunityId && x.CommunityUser.Role == CommunityUserRole.Teacher &&
                x.CommunityUser.Status == CommunityUserStatus.Pending && x.AcceptedAt == null && x.RevokedAt == null &&
                x.ExpiresAt > DateTime.UtcNow, ct) : null
        };
    }
}
