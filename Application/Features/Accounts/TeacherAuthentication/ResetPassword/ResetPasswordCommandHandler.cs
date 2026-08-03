using Domain.Services;

using MediatR;

using Shared.Helpers;
using Shared.Enums;
using Shared.Exceptions;

using System.Net;

namespace Application.Features.Accounts.TeacherAuthentication.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly ITeacherIdentityService _identityService;
    public ResetPasswordCommandHandler(ITeacherIdentityService identityService) => _identityService = identityService;
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || !UrlSafeTokenHelper.TryDecode(request.Token, out var token))
        {
            throw new GenericException(ErrorCode.InvalidOrExpiredPasswordResetToken, ErrorMessage.InvalidOrExpiredPasswordResetToken, HttpStatusCode.BadRequest);
        }
        await _identityService.ResetPasswordAsync(request.UserId, token, request.NewPassword, cancellationToken);
    }
}