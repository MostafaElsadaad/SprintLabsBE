using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.CancelDashboardInvitation;

public class CancelDashboardInvitationCommand : IRequest<bool>
{
    public long UserId { get; set; }
    public long InvitationId { get; set; }
}
