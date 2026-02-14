using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

using Domain.Services;

using Newtonsoft.Json;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Infrastructure.Services
{
    public class GoogleAuthenticationService : IGoogleAuthenticationService
    {
        private readonly HttpClient _httpClient;

        public GoogleAuthenticationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GoogleUserResponse> GetUserInfo(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            if (userInfoResponse.IsSuccessStatusCode)
            {
                var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GoogleUserResponse>(userInfo); ;
            }
            else
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Unauthorized,
                    errorCode: ErrorCode.Failure);
            }
        }

        public List<Claim> GenerateGoogleClaims(GoogleUserResponse response)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, response.Email)
            };
        }

    }
}
