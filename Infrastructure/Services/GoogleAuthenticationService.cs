using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Domain.Services;

using Google.Apis.Auth;

using Microsoft.Extensions.Configuration;

using Shared.Responses;

namespace Infrastructure.Services
{
    public class GoogleAuthenticationService : IGoogleAuthenticationService
    {
        private readonly string[] _allowedClientIds;

        public GoogleAuthenticationService(IConfiguration configuration)
        {
            _allowedClientIds = configuration
                .GetSection("Authentication:Google:AllowedClientIds")
                .Get<string[]>() ?? Array.Empty<string>();
        }

        public async Task<GoogleUserResponse> GetUserInfo(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new UnauthorizedAccessException("ID token is required.");

            if (_allowedClientIds.Length == 0)
                throw new InvalidOperationException("Google AllowedClientIds are not configured.");

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _allowedClientIds
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(accessToken, settings);

            return new GoogleUserResponse
            {
                Sub = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture,
                Domain = payload.HostedDomain
            };
        }

        public List<Claim> GenerateGoogleClaims(GoogleUserResponse response)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, response.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub, response.Sub ?? string.Empty),
                new Claim("name", response.Name ?? string.Empty)
            };
        }
    }
}