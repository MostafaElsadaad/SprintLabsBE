using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;
using Application.Features.Accounts.TeacherAuthentication.Common;

namespace Application.Features.CommunityDashboard.AssignDashboardTeacher;

public class AssignDashboardTeacherCommandHandler : IRequestHandler<AssignDashboardTeacherCommand, bool>
{
    private readonly DashboardAuthorization _authorization;
    private readonly ITeacherClassAssignmentService _assignments;

    public AssignDashboardTeacherCommandHandler(DashboardAuthorization authorization, ITeacherClassAssignmentService assignments)
    {
        _authorization = authorization;
        _assignments = assignments;
    }

    public async Task<bool> Handle(AssignDashboardTeacherCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        DashboardAuthorization.RequireClass(scope, request.ClassId);
        await _assignments.AssignClassAsync(request.UserId, scope.CommunityId, request.ClassId, request.TeacherId, ct);
        return true;
    }
}
