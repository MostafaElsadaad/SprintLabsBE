using System.Security.Claims;

using Shared.Responses;

namespace Domain.Services
{
    public interface IGoogleAuthenticationService
    {
        public Task<GoogleUserResponse> GetUserInfo(string accessToken);
        public List<Claim> GenerateGoogleClaims(GoogleUserResponse response);
    }
}
