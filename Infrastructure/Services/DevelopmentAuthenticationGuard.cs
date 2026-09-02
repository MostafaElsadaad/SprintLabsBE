using System.Net;
using System.Security.Cryptography;
using System.Text;

using Domain.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;

namespace Infrastructure.Services;

public sealed class DevelopmentAuthenticationGuard : IDevelopmentAuthenticationGuard
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DevelopmentAuthenticationOptions _options;

    public DevelopmentAuthenticationGuard(
        IHostEnvironment hostEnvironment,
        IOptions<DevelopmentAuthenticationOptions> options)
    {
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public bool CanSeed => IsAvailable && _options.SeedPlayers;

    public void EnsureEndpointAccess(IReadOnlyCollection<string> suppliedApiKeys)
    {
        if (!IsAvailable)
        {
            throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        }

        var configuredApiKey = _options.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return;
        }

        if (suppliedApiKeys.Count != 1)
        {
            throw Unauthorized();
        }

        var suppliedApiKey = suppliedApiKeys.Single();
        if (string.IsNullOrWhiteSpace(suppliedApiKey) || !FixedTimeEquals(configuredApiKey, suppliedApiKey))
        {
            throw Unauthorized();
        }
    }

    private bool IsAvailable => !_hostEnvironment.IsProduction() && _options.Enabled;

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static GenericException Unauthorized() =>
        new(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized);
}
