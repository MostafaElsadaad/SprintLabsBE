using Domain.Services;

using FluentAssertions;

using Infrastructure.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Exceptions;
using Shared.Options;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentAuthenticationGuardTests
{
    [Theory]
    [InlineData("Production", true, true)]
    [InlineData("Development", false, true)]
    [InlineData("Development", true, false)]
    public void CanSeed_IsFalseUnlessExplicitlyEnabledOutsideProduction(string environment, bool enabled, bool seedPlayers)
    {
        var guard = Create(environment, enabled, seedPlayers);

        guard.CanSeed.Should().BeFalse();
    }

    [Fact]
    public void CanSeed_IsTrueWhenBothFlagsAreEnabledOutsideProduction()
    {
        Create("Development", true, true).CanSeed.Should().BeTrue();
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", false)]
    public void EnsureEndpointAccess_ConcealsUnavailableFeature(string environment, bool enabled)
    {
        var guard = Create(environment, enabled, seedPlayers: true);

        var action = () => guard.EnsureEndpointAccess(Array.Empty<string>());

        action.Should().Throw<GenericException>()
            .Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public void EnsureEndpointAccess_AllowsEnabledNonProductionWithoutConfiguredKey()
    {
        var guard = Create("Staging", enabled: true, seedPlayers: false);

        var action = () => guard.EnsureEndpointAccess(Array.Empty<string>());

        action.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(InvalidKeys))]
    public void EnsureEndpointAccess_RejectsMissingDuplicateOrInvalidConfiguredKey(string[] values)
    {
        var guard = Create("Development", enabled: true, seedPlayers: true, apiKey: "correct-development-key");

        var action = () => guard.EnsureEndpointAccess(values);

        action.Should().Throw<GenericException>()
            .Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void EnsureEndpointAccess_AllowsExactlyOneMatchingConfiguredKey()
    {
        var guard = Create("Development", enabled: true, seedPlayers: true, apiKey: "correct-development-key");

        var action = () => guard.EnsureEndpointAccess(["correct-development-key"]);

        action.Should().NotThrow();
    }

    public static TheoryData<string[]> InvalidKeys => new()
    {
        Array.Empty<string>(),
        new[] { "" },
        new[] { "wrong-development-key" },
        new[] { "correct-development-key", "correct-development-key" }
    };

    private static IDevelopmentAuthenticationGuard Create(
        string environment,
        bool enabled,
        bool seedPlayers,
        string? apiKey = null)
    {
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.EnvironmentName).Returns(environment);
        return new DevelopmentAuthenticationGuard(
            hostEnvironment.Object,
            Options.Create(new DevelopmentAuthenticationOptions
            {
                Enabled = enabled,
                SeedPlayers = seedPlayers,
                ApiKey = apiKey
            }));
    }
}
