using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.SearchDashboard;

public class SearchDashboardQueryHandler : IRequestHandler<SearchDashboardQuery, List<SearchItem>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public SearchDashboardQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<List<SearchItem>> Handle(SearchDashboardQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        if (string.IsNullOrWhiteSpace(request.Q)) return new List<SearchItem>();
        var text = request.Q.Trim();
        if (text.Length > 100) throw DashboardAuthorization.Invalid();
        var students = (await _projection.StudentsAsync(scope, null, ct))
            .Where(x => x.FullName.Contains(text, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Select(x => new SearchItem { Id = x.Id, Name = x.FullName, Type = "student" });
        var rows = students.ToList();
        if (scope.IsOwner)
        {
            rows.AddRange((await _projection.ClassesAsync(scope, ct)).Where(x => x.Status != "ARCHIVED" &&
                x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(x => new SearchItem { Id = x.Id, Type = "class", Name = x.Name }));
            rows.AddRange((await _projection.TeachersAsync(scope, ct)).Where(x => x.Status != "INACTIVE" &&
                (x.FullName.Contains(text, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(text, StringComparison.OrdinalIgnoreCase)))
                .Select(x => new SearchItem { Id = x.Id, Type = "teacher", Name = x.FullName }));
        }
        return rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Type).ThenBy(x => x.Id).Take(50).ToList();
    }
}
