using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.TeacherActivity;

public class TeacherActivityQueryHandler : IRequestHandler<TeacherActivityQuery, PagedResponse<TeacherActivityResponse>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<StaffActivity> _activities;

    public TeacherActivityQueryHandler(DashboardAuthorization authorization, DashboardProjection projection, IBaseRepository<StaffActivity> activities)
    {
        _authorization = authorization;
        _projection = projection;
        _activities = activities;
    }

    public async Task<PagedResponse<TeacherActivityResponse>> Handle(TeacherActivityQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        DashboardPaging.Validate(request.PageNumber, request.PageSize);
        var (from, to) = DashboardPeriod.Range(request.From, request.To);
        var teachers = await _projection.TeachersAsync(scope, ct);
        var events = await _activities.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId &&
            x.CreatedAt >= from && x.CreatedAt < to).GroupBy(x => x.TeacherUserId)
            .Select(x => new { Id = x.Key, Count = x.Count(), Last = x.Max(e => e.CreatedAt) }).ToDictionaryAsync(x => x.Id, ct);
        var rows = teachers.Where(x => x.MembershipStatus != "Removed").OrderBy(x => x.FullName).ThenBy(x => x.Id).Select(x =>
            new TeacherActivityResponse { TeacherId = x.Id, FullName = x.FullName, ClassesCount = x.Classes.Count,
                ObservedEventsCount = events.GetValueOrDefault(x.Id)?.Count ?? 0,
                LastObservedActivityAt = events.GetValueOrDefault(x.Id)?.Last });
        return DashboardPaging.Page(rows, request.PageNumber, request.PageSize);
    }
}
