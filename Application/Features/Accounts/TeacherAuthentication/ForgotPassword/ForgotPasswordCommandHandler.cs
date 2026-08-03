using Application.Features.Accounts.TeacherAuthentication.Common;

using Domain.Services;

using MediatR;

using Microsoft.Extensions.Options;

using Shared.Options;

namespace Application.Features.Accounts.TeacherAuthentication.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;
    public ForgotPasswordCommandHandler(ITeacherIdentityService identityService, IEmailService emailService, IOptions<FrontendOptions> frontendOptions)
    { _identityService = identityService; _emailService = emailService; _frontendOptions = frontendOptions.Value; }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var dispatch = await _identityService.CreatePasswordResetAsync(request.Identifier, cancellationToken);
        if (dispatch.UserId > 0 && dispatch.Email != null && dispatch.Name != null && dispatch.ResetToken != null)
        {
            try
            {
                await _emailService.SendPasswordResetEmailAsync(dispatch.Email, dispatch.Name,
                    TeacherAuthenticationLinkBuilder.PasswordReset(_frontendOptions, dispatch.UserId, dispatch.ResetToken), cancellationToken);
            }
            catch
            {
                // The public response remains generic so delivery state cannot enumerate accounts.
            }
        }
    }
}