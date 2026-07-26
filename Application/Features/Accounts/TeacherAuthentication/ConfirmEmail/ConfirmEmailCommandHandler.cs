using Domain.Services;

using MediatR;

using Shared.Helpers;

namespace Application.Features.Accounts.TeacherAuthentication.ConfirmEmail;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly ITeacherIdentityService _identityService;
    public ConfirmEmailCommandHandler(ITeacherIdentityService identityService) => _identityService = identityService;

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || !UrlSafeTokenHelper.TryDecode(request.Token, out var token)) throw new ArgumentException("Invalid confirmation token.");
        await _identityService.ConfirmEmailAsync(request.UserId, token, cancellationToken);
    }
}
