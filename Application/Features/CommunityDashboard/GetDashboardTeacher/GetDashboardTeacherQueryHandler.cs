using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.GetDashboardTeacher;

public class GetDashboardTeacherQueryHandler : IRequestHandler<GetDashboardTeacherQuery, TeacherView>
{
    private readonly DashboardAuthorization _authorization;
    private readonly DashboardProjection _projection;

    public GetDashboardTeacherQueryHandler(DashboardAuthorization authorization, DashboardProjection projection)
    {
        _authorization = authorization;
        _projection = projection;
    }

    public async Task<TeacherView> Handle(GetDashboardTeacherQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        return (await _projection.TeachersAsync(scope, ct)).SingleOrDefault(x => x.Id == request.TeacherId)
            ?? throw DashboardAuthorization.NotFound();
    }
}
