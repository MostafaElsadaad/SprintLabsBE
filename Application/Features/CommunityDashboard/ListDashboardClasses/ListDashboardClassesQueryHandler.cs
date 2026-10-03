using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardClasses;

public class ListDashboardClassesQueryHandler : IRequestHandler<ListDashboardClassesQuery, PagedResponse<ClassView>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public ListDashboardClassesQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<PagedResponse<ClassView>> Handle(ListDashboardClassesQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        DashboardPaging.Validate(request.PageNumber, request.PageSize);
        var descending = DashboardPaging.Descending(request.Sort, "name", "createdAt", "studentsCount");
        var rows = (await _projection.ClassesAsync(scope, ct)).AsEnumerable();
        if (request.Status != null && !new[] { "ACTIVE", "NO_TEACHER", "ARCHIVED" }.Contains(request.Status))
            throw DashboardAuthorization.Invalid();
        rows = request.Status != null ? rows.Where(x => x.Status == request.Status) : rows.Where(x => x.Status != "ARCHIVED");
        if (request.GradeId.HasValue) rows = rows.Where(x => x.Grade.Id == request.GradeId);
        if (request.TeacherId.HasValue) rows = rows.Where(x => x.Teachers.Any(t => t.Id == request.TeacherId));
        if (!string.IsNullOrWhiteSpace(request.Search)) rows = rows.Where(x => x.Name.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        var field = (request.Sort ?? "name:asc").Split(':')[0];
        rows = field switch
        {
            "createdAt" => descending ? rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id) : rows.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            "studentsCount" => descending ? rows.OrderByDescending(x => x.StudentsCount).ThenBy(x => x.Id) : rows.OrderBy(x => x.StudentsCount).ThenBy(x => x.Id),
            _ => descending ? rows.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id) : rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
        };
        return DashboardPaging.Page(rows, request.PageNumber, request.PageSize);
    }
}
