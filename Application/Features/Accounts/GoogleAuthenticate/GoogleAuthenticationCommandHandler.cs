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
        //private readonly IBaseRepository<UserProfile> _userProfileRepository;

        public GoogleAuthenticationCommandHandler(IUserService userService, IGoogleAuthenticationService googleAuthenticationService)
        {
            _userService = userService;
            _googleAuthenticationService = googleAuthenticationService;
            //_userProfileRepository = userRepository;
        }
        public async Task<LoginResponse> Handle(GoogleAuthenticationCommand request, CancellationToken cancellationToken)
        {
            var googleUserInfo = await _googleAuthenticationService.GetUserInfo(request.AccessToken);

            if (googleUserInfo == null || string.IsNullOrEmpty(googleUserInfo.Email) || googleUserInfo.Domain != "dsquares.com")
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Unauthorized,
                    errorCode: ErrorCode.Failure);
            }

            var userExists = await _userService.EnsureUserExists(googleUserInfo.Email);
            //if (!userExists)
            //{
            //    await RegisterNewUserWithProfile(googleUserInfo);
            //}

            List<Claim> claims = _googleAuthenticationService.GenerateGoogleClaims(googleUserInfo);

            var loginResponse = await _userService.Authenticate(claims);

            loginResponse.Email = googleUserInfo.Email;
            loginResponse.Name = googleUserInfo.Name;
            loginResponse.PictureUrl = googleUserInfo.Picture;
            return loginResponse;
        }


        //private async Task RegisterNewUserWithProfile(GoogleUserResponse googleUserInfo)
        //{

        //    var userProfile = new UserProfile
        //    {
        //        Name = googleUserInfo.Name,
        //        ImagePath = googleUserInfo.Picture
        //    };
        //    await _userProfileRepository.AddAsync(userProfile);
        //    await _userProfileRepository.SaveChangesAsync();

        //    var userId = await _userService.CreateAccount(googleUserInfo, userProfile.Id);

        //    userProfile.UserId = userId;
        //    await _userProfileRepository.UpdateAsync(userProfile);
        //    await _userProfileRepository.SaveChangesAsync();
        //}


    }
}
