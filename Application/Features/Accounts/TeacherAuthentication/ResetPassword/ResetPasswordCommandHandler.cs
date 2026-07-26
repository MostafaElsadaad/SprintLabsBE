using Domain.Services;
using MediatR;
using Shared.Helpers;

namespace Application.Features.Accounts.TeacherAuthentication.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly ITeacherIdentityService _identityService;
    public ResetPasswordCommandHandler(ITeacherIdentityService identityService) => _identityService = identityService;
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!UrlSafeTokenHelper.TryDecode(request.Token, out var token)) throw new ArgumentException("Invalid reset token.");
        await _identityService.ResetPasswordAsync(request.Email, token, request.NewPassword, cancellationToken);
    }
}
