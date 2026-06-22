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
            if (googleUserInfo == null || string.IsNullOrEmpty(googleUserInfo.Email) || string.IsNullOrEmpty(googleUserInfo.Sub))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Unauthorized,
                    errorCode: ErrorCode.Failure);
            }

            // 2. Find or create shared login identity
            var user = await _userService.FindOrCreateGoogleUser(googleUserInfo);
            if (user.IsSuspended)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Forbidden,
                    errorCode: ErrorCode.Failure);
            }

            // 3. Find or create Player profile for game login
            var player = await _playerRepository.GetByUserIdAsync(user.Id);
            if (player == null)
            {
                player = await _playerRepository.GetByGoogleIdAsync(googleUserInfo.Sub);
                if (player != null)
                {
                    if (player.UserId.HasValue && player.UserId.Value != user.Id)
                    {
                        throw new GenericException(
                            message: ErrorMessage.ExistingRecord,
                            statusCode: HttpStatusCode.Conflict,
                            errorCode: ErrorCode.Failure);
                    }

                    player.UserId = user.Id;
                    player.Email = googleUserInfo.Email;
                    player.Name = googleUserInfo.Name;
                    player.AvatarUrl = googleUserInfo.Picture;
                    player = await _playerRepository.UpdatePlayer(player);
                }
                else
                {
                    // First time game login creates a player profile with default progression.
                    player = await _playerRepository.CreateAsync(new Player
                    {
                        UserId = user.Id,
                        GoogleId = googleUserInfo.Sub,
                        Email = googleUserInfo.Email,
                        Name = googleUserInfo.Name,
                        AvatarUrl = googleUserInfo.Picture,

                    });
                }
            }

            // 4. Generate JWT
            List<Claim> claims = _googleAuthenticationService.GenerateGoogleClaims(googleUserInfo);
            claims.Add(new Claim("userId", user.Id.ToString()));
            claims.Add(new Claim("playerProfileId", player.Id.ToString()));
            var loginResponse = await _userService.Authenticate(claims);

            // 5. Return response
            loginResponse.UserId = user.Id;
            loginResponse.PlayerProfileId = player.Id;
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