using System.Net;

using Application.Features.Accounts.Common;

using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.FirebaseAuthenticate;

public class FirebaseAuthenticationCommandHandler : IRequestHandler<FirebaseAuthenticationCommand, LoginResponse>
{
    private readonly IFirebaseAuthenticationService _firebaseAuthenticationService;
    private readonly IUserService _userService;
    private readonly IExternalPlayerLoginWorkflow _externalPlayerLoginWorkflow;

    public FirebaseAuthenticationCommandHandler(
        IFirebaseAuthenticationService firebaseAuthenticationService,
        IUserService userService,
        IExternalPlayerLoginWorkflow externalPlayerLoginWorkflow)
    {
        _firebaseAuthenticationService = firebaseAuthenticationService;
        _userService = userService;
        _externalPlayerLoginWorkflow = externalPlayerLoginWorkflow;
    }

    public async Task<LoginResponse> Handle(FirebaseAuthenticationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        }

        var firebaseUser = await _firebaseAuthenticationService.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(firebaseUser.Uid) || string.IsNullOrWhiteSpace(firebaseUser.Email))
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
        }

        var user = await _userService.FindOrCreateFirebaseUser(firebaseUser, cancellationToken);
        return await _externalPlayerLoginWorkflow.CompleteAsync(
            user,
            new ExternalPlayerLoginContext
            {
                Subject = firebaseUser.Uid,
                Email = firebaseUser.Email.Trim(),
                EmailVerified = firebaseUser.EmailVerified,
                Name = string.IsNullOrWhiteSpace(firebaseUser.Name) ? firebaseUser.Email.Trim() : firebaseUser.Name.Trim(),
                PictureUrl = firebaseUser.PictureUrl ?? string.Empty,
                GoogleProviderId = firebaseUser.GoogleProviderId,
                ActivatePendingTeacherMemberships = true
            },
            cancellationToken);
    }
}
