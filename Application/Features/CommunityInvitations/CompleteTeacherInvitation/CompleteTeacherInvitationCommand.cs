using MediatR;

namespace Application.Features.CommunityInvitations.CompleteTeacherInvitation;

public class CompleteTeacherInvitationCommand : IRequest
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
