using MediatR;
using Shared.Responses;

namespace Application.Features.CommunityInvitations.AcceptInvitation;

public class AcceptInvitationCommand : IRequest<TeacherInvitationAcceptanceResponse>
{
    public long UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}
