using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;
using Application.Features.Communities.Students.Common;

namespace Application.Features.CommunityDashboard.ListDashboardStudents;

public class ListDashboardStudentsQueryHandler : IRequestHandler<ListDashboardStudentsQuery, PagedResponse<StudentView>>
{
    private readonly StudentRosterService _roster;

    public ListDashboardStudentsQueryHandler(StudentRosterService roster)
    {
        _roster = roster;
    }

    public async Task<PagedResponse<StudentView>> Handle(ListDashboardStudentsQuery request, CancellationToken ct)
    {
        DashboardPaging.Validate(request.PageNumber, request.PageSize);
        var descending = DashboardPaging.Descending(request.Sort, "name");
        var rows = (await _roster.ReadAsync(request.UserId, request.ClassId, request.GradeId, request.Search, request.Status, ct)).AsEnumerable();
        rows = descending ? rows.OrderByDescending(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id) :
            rows.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);
        return DashboardPaging.Page(rows, request.PageNumber, request.PageSize);
    }
}
