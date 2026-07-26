using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Domain.Services;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shared.Options;
using Shared.Responses;

namespace Infrastructure.Services;

public class AccessTokenService : IAccessTokenService
{
    private readonly JWTOptions _options;

    public AccessTokenService(IOptions<JWTOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult Create(long userId, string email, string name)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("userId", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", name),
            new Claim("accountType", "Teacher")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha512));

        return new AccessTokenResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            AccessTokenExpiresAt = expiresAt
        };
    }
}
