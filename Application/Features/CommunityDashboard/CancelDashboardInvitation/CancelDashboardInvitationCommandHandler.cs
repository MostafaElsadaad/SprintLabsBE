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

namespace Application.Features.CommunityDashboard.CancelDashboardInvitation;

public class CancelDashboardInvitationCommandHandler : IRequestHandler<CancelDashboardInvitationCommand, bool>
{
    private readonly DashboardAuthorization _authorization;
    private readonly ITeacherInvitationService _invitations;

    public CancelDashboardInvitationCommandHandler(DashboardAuthorization authorization, ITeacherInvitationService invitations)
    {
        _authorization = authorization;
        _invitations = invitations;
    }

    public async Task<bool> Handle(CancelDashboardInvitationCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        if (request.InvitationId <= 0) throw DashboardAuthorization.Invalid();
        await _invitations.CancelAsync(request.UserId, scope.CommunityId, request.InvitationId, ct);
        return true;
    }
}
