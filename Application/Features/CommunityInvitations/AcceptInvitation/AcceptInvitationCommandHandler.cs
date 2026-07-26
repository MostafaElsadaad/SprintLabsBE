using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.CommunityInvitations.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, TeacherInvitationAcceptanceResponse>
{
    private readonly ITeacherInvitationService _invitationService;
    public AcceptInvitationCommandHandler(ITeacherInvitationService invitationService) => _invitationService = invitationService;
    public Task<TeacherInvitationAcceptanceResponse> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
        => _invitationService.AcceptAsync(request.UserId, request.Token, cancellationToken);
}
