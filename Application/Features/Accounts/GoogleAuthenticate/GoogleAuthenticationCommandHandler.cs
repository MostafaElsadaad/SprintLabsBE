using System.Net;
using System.Security.Claims;

using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Accounts.GoogleAuthenticate
{
    public class GoogleAuthenticationCommandHandler : IRequestHandler<GoogleAuthenticationCommand, LoginResponse>
    {
        private readonly IUserService _userService;
        private readonly IGoogleAuthenticationService _googleAuthenticationService;

        public GoogleAuthenticationCommandHandler(
            IUserService userService,
            IGoogleAuthenticationService googleAuthenticationService)
        {
            _userService = userService;
            _googleAuthenticationService = googleAuthenticationService;
        }

        public async Task<LoginResponse> Handle(GoogleAuthenticationCommand request, CancellationToken cancellationToken)
        {
            var googleUserInfo = await _googleAuthenticationService.GetUserInfo(request.IdToken);

            if (googleUserInfo == null || string.IsNullOrEmpty(googleUserInfo.Email))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Unauthorized,
                    errorCode: ErrorCode.Failure);
            }

            var userExists = await _userService.EnsureUserExists(googleUserInfo.Email);

            List<Claim> claims = _googleAuthenticationService.GenerateGoogleClaims(googleUserInfo);

            var loginResponse = await _userService.Authenticate(claims);

            loginResponse.Email = googleUserInfo.Email;
            loginResponse.Name = googleUserInfo.Name;
            loginResponse.PictureUrl = googleUserInfo.Picture;

            return loginResponse;
        }
    }
}