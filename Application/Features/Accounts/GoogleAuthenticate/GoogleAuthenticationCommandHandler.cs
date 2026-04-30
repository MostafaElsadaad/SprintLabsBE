using System.Net;
using System.Security.Claims;

using Domain.Models;
using Domain.Repositories;
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
        private readonly IPlayerRepository _playerRepository;

        public GoogleAuthenticationCommandHandler(
            IUserService userService,
            IGoogleAuthenticationService googleAuthenticationService,
            IPlayerRepository playerRepository)
        {
            _userService = userService;
            _googleAuthenticationService = googleAuthenticationService;
            _playerRepository = playerRepository;
        }

        public async Task<LoginResponse> Handle(GoogleAuthenticationCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify Google token
            var googleUserInfo = await _googleAuthenticationService.GetUserInfo(request.IdToken);
            if (googleUserInfo == null || string.IsNullOrEmpty(googleUserInfo.Email))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Unauthorized,
                    errorCode: ErrorCode.Failure);
            }

            // 2. Find or create Player
            var player = await _playerRepository.GetByGoogleIdAsync(googleUserInfo.Sub);
            if (player == null)
            {
                // First time login — create new player with default progression
                player = await _playerRepository.CreateAsync(new Player
                {
                    GoogleId = googleUserInfo.Sub,
                    Email = googleUserInfo.Email,
                    Name = googleUserInfo.Name,
                    AvatarUrl = googleUserInfo.Picture,
          
                });
            }

            // 3. Generate JWT
            List<Claim> claims = _googleAuthenticationService.GenerateGoogleClaims(googleUserInfo);
            var loginResponse = await _userService.Authenticate(claims);

            // 4. Return response
            loginResponse.Email = googleUserInfo.Email;
            loginResponse.Name = googleUserInfo.Name;
            loginResponse.PictureUrl = googleUserInfo.Picture;
            loginResponse.Gold = player.Gold;
            loginResponse.Experience = player.Experience;
            loginResponse.Level = player.Level;

            return loginResponse;
        }


    }
}