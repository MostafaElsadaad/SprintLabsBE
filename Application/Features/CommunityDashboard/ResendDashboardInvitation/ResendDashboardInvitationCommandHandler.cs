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

namespace Application.Features.CommunityDashboard.ResendDashboardInvitation;

public class ResendDashboardInvitationCommandHandler : IRequestHandler<ResendDashboardInvitationCommand, bool>
{
    private readonly DashboardAuthorization _authorization;
    private readonly ITeacherInvitationService _invitations;
    private readonly IEmailService _email;
    private readonly FrontendOptions _frontend;

    public ResendDashboardInvitationCommandHandler(DashboardAuthorization authorization, ITeacherInvitationService invitations, IEmailService email, IOptions<FrontendOptions> frontend)
    {
        _authorization = authorization;
        _invitations = invitations; _email = email; _frontend = frontend.Value;
    }

    public async Task<bool> Handle(ResendDashboardInvitationCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        if (request.InvitationId <= 0) throw DashboardAuthorization.Invalid();
        var issue = await _invitations.ResendAsync(request.UserId, scope.CommunityId, request.InvitationId, ct);
        if (!string.IsNullOrWhiteSpace(issue.InvitationToken))
            await _email.SendCommunityInvitationEmailAsync(issue.Email, issue.Name, issue.CommunityName,
                TeacherAuthenticationLinkBuilder.Invitation(_frontend, issue.InvitationToken), ct);
        return true;
    }
}
