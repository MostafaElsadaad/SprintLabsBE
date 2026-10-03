using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardTeachers;

public class ListDashboardTeachersQueryHandler : IRequestHandler<ListDashboardTeachersQuery, PagedResponse<TeacherView>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public ListDashboardTeachersQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<PagedResponse<TeacherView>> Handle(ListDashboardTeachersQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        DashboardPaging.Validate(request.PageNumber, request.PageSize);
        var descending = DashboardPaging.Descending(request.Sort, "name", "joinedAt");
        if (request.Title != null && !new[] { "TEACHER", "LEAD_TEACHER" }.Contains(request.Title)) throw DashboardAuthorization.Invalid();
        if (request.Status != null && !new[] { "ACTIVE", "INACTIVE", "PENDING" }.Contains(request.Status)) throw DashboardAuthorization.Invalid();
        var rows = (await _projection.TeachersAsync(scope, ct)).AsEnumerable();
        if (request.Title != null) rows = rows.Where(x => x.Title == request.Title);
        if (request.Status != null) rows = rows.Where(x => x.Status == request.Status);
        if (request.GradeId.HasValue) rows = rows.Where(x => x.Grades.Any(g => g.Id == request.GradeId));
        if (request.ClassId.HasValue) rows = rows.Where(x => x.Classes.Any(c => c.Id == request.ClassId));
        if (!string.IsNullOrWhiteSpace(request.Search)) rows = rows.Where(x =>
            x.FullName.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.Email.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.TeacherCode.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        rows = (request.Sort ?? "name:asc").StartsWith("joinedAt:") ?
            descending ? rows.OrderByDescending(x => x.JoinedAt).ThenBy(x => x.Id) : rows.OrderBy(x => x.JoinedAt).ThenBy(x => x.Id) :
            descending ? rows.OrderByDescending(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id) : rows.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);
        return DashboardPaging.Page(rows, request.PageNumber, request.PageSize);
    }
}
