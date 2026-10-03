using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.GetDashboardClass;

public class GetDashboardClassQueryHandler : IRequestHandler<GetDashboardClassQuery, ClassDetail>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public GetDashboardClassQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<ClassDetail> Handle(GetDashboardClassQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, false, ct);
        DashboardAuthorization.RequireClass(scope, request.ClassId);
        var row = (await _projection.ClassesAsync(scope, ct)).Single(x => x.Id == request.ClassId);
        return new ClassDetail
        {
            IsOwner = scope.IsOwner, TeacherName = row.Teachers.FirstOrDefault(x => x.Id == request.UserId)?.FullName ?? string.Empty,
            Id = row.Id, Name = row.Name, Grade = row.Grade, Teachers = row.Teachers,
            StudentsCount = row.StudentsCount, CreatedAt = row.CreatedAt, Status = row.Status,
            StudentsPreview = (await _projection.StudentsAsync(scope, request.ClassId, ct))
                .OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id).Take(4).ToList()
        };
    }
}
