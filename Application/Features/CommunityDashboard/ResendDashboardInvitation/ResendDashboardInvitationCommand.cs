using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ResendDashboardInvitation;

public class ResendDashboardInvitationCommand : IRequest<bool>
{
    public long UserId { get; set; }
    public long InvitationId { get; set; }
}
