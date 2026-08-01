using System.Net;

using Application.Features.Accounts.Common;

using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.GoogleAuthenticate;

public class GoogleAuthenticationCommandHandler : IRequestHandler<GoogleAuthenticationCommand, LoginResponse>
{
    private readonly IUserService _userService;
    private readonly IGoogleAuthenticationService _googleAuthenticationService;
    private readonly IExternalPlayerLoginWorkflow _externalPlayerLoginWorkflow;

    public GoogleAuthenticationCommandHandler(
        IUserService userService,
        IGoogleAuthenticationService googleAuthenticationService,
        IExternalPlayerLoginWorkflow externalPlayerLoginWorkflow)
    {
        _userService = userService;
        _googleAuthenticationService = googleAuthenticationService;
        _externalPlayerLoginWorkflow = externalPlayerLoginWorkflow;
    }

    public async Task<LoginResponse> Handle(GoogleAuthenticationCommand request, CancellationToken cancellationToken)
    {
        var googleUserInfo = await _googleAuthenticationService.GetUserInfo(request.IdToken);
        if (googleUserInfo == null || string.IsNullOrWhiteSpace(googleUserInfo.Email) || string.IsNullOrWhiteSpace(googleUserInfo.Sub))
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        }

        var user = await _userService.FindOrCreateGoogleUser(googleUserInfo);
        return await _externalPlayerLoginWorkflow.CompleteAsync(
            user,
            new ExternalPlayerLoginContext
            {
                Subject = googleUserInfo.Sub,
                Email = googleUserInfo.Email,
                EmailVerified = true,
                Name = googleUserInfo.Name ?? string.Empty,
                PictureUrl = googleUserInfo.Picture ?? string.Empty,
                GoogleProviderId = googleUserInfo.Sub,
                ActivatePendingTeacherMemberships = false
            },
            cancellationToken);
    }
}
