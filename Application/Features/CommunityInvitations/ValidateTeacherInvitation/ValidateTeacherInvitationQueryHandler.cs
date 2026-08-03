using Domain.Services;

using MediatR;

using Shared.Responses;

namespace Application.Features.CommunityInvitations.ValidateTeacherInvitation;

public class ValidateTeacherInvitationQueryHandler : IRequestHandler<ValidateTeacherInvitationQuery, TeacherInvitationValidationResult>
{
    private readonly ITeacherInvitationService _invitationService;

    public ValidateTeacherInvitationQueryHandler(ITeacherInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    public Task<TeacherInvitationValidationResult> Handle(ValidateTeacherInvitationQuery request, CancellationToken cancellationToken)
    {
        return _invitationService.ValidateAsync(request.Token, cancellationToken);
    }
}
