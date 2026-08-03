using Domain.Services;

using MediatR;

namespace Application.Features.CommunityInvitations.CompleteTeacherInvitation;

public class CompleteTeacherInvitationCommandHandler : IRequestHandler<CompleteTeacherInvitationCommand>
{
    private readonly ITeacherInvitationService _invitationService;

    public CompleteTeacherInvitationCommandHandler(ITeacherInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    public Task Handle(CompleteTeacherInvitationCommand request, CancellationToken cancellationToken)
    {
        return _invitationService.CompleteAsync(request.Token, request.Name, request.Password, cancellationToken);
    }
}
