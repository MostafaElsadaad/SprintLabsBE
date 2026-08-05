using Domain.Services;

using MediatR;

namespace Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;

public class RegisterCommunityCommandHandler : IRequestHandler<RegisterCommunityCommand>
{
    private readonly ITeacherInvitationService _invitationService;

    public RegisterCommunityCommandHandler(ITeacherInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    public Task Handle(RegisterCommunityCommand request, CancellationToken cancellationToken)
    {
        return _invitationService.CompleteAsync(request.Token, request.Name, request.Password, cancellationToken);
    }
}
