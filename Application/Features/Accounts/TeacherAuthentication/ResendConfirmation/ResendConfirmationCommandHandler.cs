using Application.Features.Accounts.TeacherAuthentication.Common;

using Domain.Services;

using MediatR;

using Microsoft.Extensions.Options;

using Shared.Options;

namespace Application.Features.Accounts.TeacherAuthentication.ResendConfirmation;

public class ResendConfirmationCommandHandler : IRequestHandler<ResendConfirmationCommand, ResendConfirmationResponse>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;
    public ResendConfirmationCommandHandler(ITeacherIdentityService identityService, IEmailService emailService, IOptions<FrontendOptions> frontendOptions)
    {
        _identityService = identityService; _emailService = emailService; _frontendOptions = frontendOptions.Value;
    }

    public async Task<ResendConfirmationResponse> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var dispatch = await _identityService.ResendConfirmationAsync(request.Email, cancellationToken);
        if (!string.IsNullOrWhiteSpace(dispatch.ConfirmationToken) && dispatch.Email != null && dispatch.Name != null)
        {
            await _emailService.SendConfirmationEmailAsync(dispatch.Email, dispatch.Name, TeacherAuthenticationLinkBuilder.Confirmation(_frontendOptions, dispatch.UserId, dispatch.ConfirmationToken), cancellationToken);
        }
        return new ResendConfirmationResponse { ResendAvailableAt = dispatch.ResendAvailableAt };
    }
}
