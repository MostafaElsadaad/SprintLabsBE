using MediatR;
using Shared.Responses;

namespace Application.Features.CommunityInvitations.ValidateTeacherInvitation;

public class ValidateTeacherInvitationQuery : IRequest<TeacherInvitationValidationResult>
{
    public string Token { get; set; } = string.Empty;
}
