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

namespace Application.Features.CommunityDashboard.UpdateDashboardTeacher;

public class UpdateDashboardTeacherCommandHandler : IRequestHandler<UpdateDashboardTeacherCommand, TeacherView>
{
    private readonly DashboardAuthorization _authorization;
    private readonly ITeacherProfileService _profiles;
    private readonly IMediator _mediator;

    public UpdateDashboardTeacherCommandHandler(DashboardAuthorization authorization, ITeacherProfileService profiles, IMediator mediator)
    {
        _authorization = authorization;
        _profiles = profiles; _mediator = mediator;
    }

    public async Task<TeacherView> Handle(UpdateDashboardTeacherCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        if (request.Body.Grade != null) throw DashboardAuthorization.Invalid();
        await _profiles.UpdateAsync(request.UserId, scope.CommunityId, request.TeacherId,
            request.Body.FullName, request.Body.Email, request.Body.Title, ct);
        return await _mediator.Send(new GetDashboardTeacher.GetDashboardTeacherQuery { UserId = request.UserId, TeacherId = request.TeacherId }, ct);
    }
}
