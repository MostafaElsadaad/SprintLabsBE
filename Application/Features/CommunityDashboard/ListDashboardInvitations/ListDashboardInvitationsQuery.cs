using MediatR;
using Shared.Responses;

namespace Application.Features.CommunityDashboard.ListDashboardInvitations;

public class ListDashboardInvitationsQuery : IRequest<List<InvitationView>>
{
    public long UserId { get; set; }
}
