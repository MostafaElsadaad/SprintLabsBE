using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardGrades;

public class ListDashboardGradesQueryHandler : IRequestHandler<ListDashboardGradesQuery, List<GradeView>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;
    private readonly IBaseRepository<Grade> _grades;

    public ListDashboardGradesQueryHandler(DashboardAuthorization authorization, DashboardProjection projection, IBaseRepository<Grade> grades)
    {
        _authorization = authorization;
        _projection = projection;
        _grades = grades;
    }

    public async Task<List<GradeView>> Handle(ListDashboardGradesQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        var grades = await _grades.AsQueryable().AsNoTracking().Where(x => x.CommunityId == scope.CommunityId &&
            x.Value >= 7 && x.Value <= 12).OrderBy(x => x.Value).ToListAsync(ct);
        var classes = (await _projection.ClassesAsync(scope, ct)).Where(x => x.Status != "ARCHIVED").ToList();
        return grades.Select(g => new GradeView
        {
            Id = g.Id, Value = g.Value!.Value, Name = g.Name,
            ClassesCount = classes.Count(c => c.Grade.Id == g.Id),
            StudentsCount = classes.Where(c => c.Grade.Id == g.Id).Sum(c => c.StudentsCount)
        }).ToList();
    }
}
