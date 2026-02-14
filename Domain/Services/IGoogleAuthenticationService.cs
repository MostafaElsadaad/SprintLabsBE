using System.Security.Claims;

using Shared.Responses;

namespace Domain.Services
{
    public interface IGoogleAuthenticationService
    {
        Task<GoogleUserResponse> GetUserInfo(string accessToken);
        List<Claim> GenerateGoogleClaims(GoogleUserResponse response);
    }
}
