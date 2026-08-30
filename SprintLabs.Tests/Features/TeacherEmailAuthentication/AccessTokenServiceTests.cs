using System.IdentityModel.Tokens.Jwt;

using FluentAssertions;

using Infrastructure.Services;

using Microsoft.Extensions.Options;

using Shared.Options;
using Shared.Enums;

namespace Compass.Tests.Features.TeacherEmailAuthentication;

public class AccessTokenServiceTests
{
    [Fact]
    public void Create_issues_utc_jti_teacher_token_without_community_role()
    {
        var options = Options.Create(new JWTOptions
        {
            Secret = "a-very-long-test-signing-key-that-is-not-used-in-production-1234567890-extra",
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenLifetimeMinutes = 15
        });
        var result = new AccessTokenService(options).Create(42, "teacher@example.com", "Teacher");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(x => x.Type == "userId" && x.Value == "42");
        token.Claims.Should().NotContain(x =>
            x.Type.Contains("community", StringComparison.OrdinalIgnoreCase) ||
            x.Type.Contains("role", StringComparison.OrdinalIgnoreCase));
        result.AccessTokenExpiresAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_issues_platform_admin_claim_without_community_role()
    {
        var options = Options.Create(new JWTOptions
        {
            Secret = "a-very-long-test-signing-key-that-is-not-used-in-production-1234567890-extra",
            Issuer = "issuer",
            Audience = "audience",
            AccessTokenLifetimeMinutes = 15
        });

        var result = new AccessTokenService(options).Create(42, "admin@example.com", "Admin", AuthenticatedAccountType.PlatformAdmin);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(x => x.Type == "accountType" && x.Value == "PlatformAdmin");
        token.Claims.Should().Contain(x => x.Type == "userId" && x.Value == "42");
        token.Claims.Should().NotContain(x => x.Type.Contains("community", StringComparison.OrdinalIgnoreCase) || x.Type.Contains("role", StringComparison.OrdinalIgnoreCase));
    }
}
