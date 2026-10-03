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

namespace Application.Features.CommunityDashboard.WriteDashboardClass;

public class WriteDashboardClassCommandHandler : IRequestHandler<WriteDashboardClassCommand, ClassDetail>
{
    private readonly DashboardAuthorization _authorization;
    private readonly ITeacherClassAssignmentService _assignments;
    private readonly IMediator _mediator;

    public WriteDashboardClassCommandHandler(DashboardAuthorization authorization, ITeacherClassAssignmentService assignments, IMediator mediator)
    {
        _authorization = authorization;
        _assignments = assignments; _mediator = mediator;
    }

    public async Task<ClassDetail> Handle(WriteDashboardClassCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        var id = await _assignments.SaveClassAsync(request.UserId, scope.CommunityId, request.ClassId, request.Body, ct);
        return await _mediator.Send(new GetDashboardClass.GetDashboardClassQuery { UserId = request.UserId, ClassId = id }, ct);
    }
}
