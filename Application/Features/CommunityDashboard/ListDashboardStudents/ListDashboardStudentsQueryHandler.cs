using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardStudents;

public class ListDashboardStudentsQueryHandler : IRequestHandler<ListDashboardStudentsQuery, PagedResponse<StudentView>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public ListDashboardStudentsQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<PagedResponse<StudentView>> Handle(ListDashboardStudentsQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        DashboardPaging.Validate(request.PageNumber, request.PageSize);
        var descending = DashboardPaging.Descending(request.Sort, "name");
        if (request.Status != null && request.Status != "UNKNOWN") throw DashboardAuthorization.Invalid();
        var rows = (await _projection.StudentsAsync(scope, request.ClassId, ct)).AsEnumerable();
        if (request.GradeId.HasValue)
        {
            var ids = (await _projection.ClassesAsync(scope, ct)).Where(x => x.Grade.Id == request.GradeId).Select(x => x.Id).ToHashSet();
            rows = rows.Where(x => ids.Contains(x.ClassId));
        }
        if (!string.IsNullOrWhiteSpace(request.Search)) rows = rows.Where(x =>
            x.FullName.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.Email.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        rows = descending ? rows.OrderByDescending(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id) :
            rows.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);
        return DashboardPaging.Page(rows, request.PageNumber, request.PageSize);
    }
}
