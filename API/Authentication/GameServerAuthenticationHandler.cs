using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace API.Authentication;

public class GameServerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "GameServer";
    private readonly IConfiguration _configuration;

    public GameServerAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration) : base(options, logger, encoder)
        => _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Configure through GameServer__ApiKey in the API process environment/secret store.
        var expected = _configuration["GameServer:ApiKey"];
        var header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(expected) || expected.Length is < 32 or > 512 || header.Count != 1)
            return Task.FromResult(AuthenticateResult.Fail("Game server authentication required."));
        var value = header.ToString();
        const string prefix = "GameServer ";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || value.Length > 4096)
            return Task.FromResult(AuthenticateResult.Fail("Game server authentication required."));
        var supplied = Encoding.UTF8.GetBytes(value[prefix.Length..]);
        var configured = Encoding.UTF8.GetBytes(expected);
        if (!CryptographicOperations.FixedTimeEquals(supplied, configured))
            return Task.FromResult(AuthenticateResult.Fail("Game server authentication required."));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("gameServer", "true") }, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
