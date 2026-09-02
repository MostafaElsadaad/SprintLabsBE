using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

using API.Controllers;
using API.Exceptions;

using Application.Features.Accounts.Common;
using Application.Features.Accounts.DevelopmentAuthentication.DevelopmentLogin;

using Asp.Versioning;

using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Seed;
using Infrastructure.Services;

using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using Moq;

using Shared.Options;
using Shared.Responses;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentAuthenticationEndToEndTests
{
    [Fact]
    public async Task DevelopmentLoginToken_AuthenticatesUsersMeWithStablePersistedIdentity()
    {
        await using var application = await TestApplication.StartAsync("Development", enabled: true, seedPlayers: true);
        var firstSeed = await application.SeedAsync();
        var secondSeed = await application.SeedAsync();
        secondSeed.Should().BeEquivalentTo(firstSeed);
        (await application.CountUsersAsync()).Should().Be(8);
        (await application.CountPlayersAsync()).Should().Be(8);

        var discovery = await application.Client.GetFromJsonAsync<BaseResponse<List<Application.Features.Accounts.DevelopmentAuthentication.ListDevelopmentPlayers.DevelopmentPlayerResponse>>>(
            "/api/v1/Account/development-players");
        discovery!.Data.Should().HaveCount(8);
        discovery.Data.Select(x => x.AccountKey).Should().Equal(Enumerable.Range(1, 8).Select(x => $"dev-player-{x:00}"));

        var firstLogin = await application.LoginAsync("dev-player-01");
        firstLogin.Data.AccessToken.Should().NotBeNullOrWhiteSpace();
        firstLogin.Data.UserId.Should().BePositive();
        firstLogin.Data.PlayerProfileId.Should().NotBeNull().And.BePositive();

        application.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.Data.AccessToken);
        var me = await application.Client.GetFromJsonAsync<BaseResponse<Application.Features.Users.GetCurrentUser.CurrentUserResponse>>(
            "/api/v1/Users/me");
        me!.Data.UserId.Should().Be(firstLogin.Data.UserId);
        me.Data.PlayerProfileId.Should().Be(firstLogin.Data.PlayerProfileId);
        application.Client.DefaultRequestHeaders.Authorization = null;

        var repeatedLogin = await application.LoginAsync("dev-player-01");
        repeatedLogin.Data.UserId.Should().Be(firstLogin.Data.UserId);
        repeatedLogin.Data.PlayerProfileId.Should().Be(firstLogin.Data.PlayerProfileId);

        var secondPlayerLogin = await application.LoginAsync("dev-player-02");
        secondPlayerLogin.Data.UserId.Should().NotBe(firstLogin.Data.UserId);
        secondPlayerLogin.Data.PlayerProfileId.Should().NotBe(firstLogin.Data.PlayerProfileId);

        var userCountBeforeUnknown = await application.CountUsersAsync();
        var playerCountBeforeUnknown = await application.CountPlayersAsync();
        var unknown = await application.Client.PostAsJsonAsync(
            "/api/v1/Account/development-login",
            new DevelopmentLoginRequest { AccountKey = "dev-player-09" });
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await application.CountUsersAsync()).Should().Be(userCountBeforeUnknown);
        (await application.CountPlayersAsync()).Should().Be(playerCountBeforeUnknown);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", false)]
    public async Task UnavailableConfiguration_RejectsBothEndpointsAndSeeding(string environment, bool enabled)
    {
        await using var application = await TestApplication.StartAsync(environment, enabled, seedPlayers: true);

        application.CanSeed.Should().BeFalse();
        (await application.Client.GetAsync("/api/v1/Account/development-players")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await application.Client.PostAsJsonAsync(
            "/api/v1/Account/development-login",
            new DevelopmentLoginRequest { AccountKey = "dev-player-01" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await application.CountUsersAsync()).Should().Be(0);
        (await application.CountPlayersAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfiguredApiKey_RequiresExactlyOneMatchingHeader()
    {
        var apiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await using var application = await TestApplication.StartAsync(
            "Development",
            enabled: true,
            seedPlayers: true,
            apiKey: apiKey);
        await application.SeedAsync();

        (await application.Client.GetAsync("/api/v1/Account/development-players")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        application.Client.DefaultRequestHeaders.Add(DevelopmentAuthenticationOptions.HeaderName, "invalid");
        (await application.Client.GetAsync("/api/v1/Account/development-players")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        application.Client.DefaultRequestHeaders.Remove(DevelopmentAuthenticationOptions.HeaderName);

        application.Client.DefaultRequestHeaders.Add(DevelopmentAuthenticationOptions.HeaderName, apiKey);
        (await application.Client.GetAsync("/api/v1/Account/development-players")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    private sealed class TestApplication : IAsyncDisposable
    {
        private readonly IHost _host;

        private TestApplication(IHost host)
        {
            _host = host;
            Client = host.GetTestClient();
        }

        public HttpClient Client { get; }

        public bool CanSeed
        {
            get
            {
                using var scope = _host.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IDevelopmentAuthenticationGuard>().CanSeed;
            }
        }

        public static async Task<TestApplication> StartAsync(
            string environment,
            bool enabled,
            bool seedPlayers,
            string? apiKey = null)
        {
            var jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var databaseName = Guid.NewGuid().ToString();
            var settings = new Dictionary<string, string?>
            {
                ["JWTOptions:Issuer"] = "development-authentication-tests",
                ["JWTOptions:Audience"] = "development-authentication-tests",
                ["JWTOptions:Secret"] = jwtSecret,
                ["JWTOptions:ClockSkewSeconds"] = "0",
                ["DevelopmentAuthentication:Enabled"] = enabled.ToString(),
                ["DevelopmentAuthentication:SeedPlayers"] = seedPlayers.ToString(),
                ["DevelopmentAuthentication:ApiKey"] = apiKey
            };

            var host = await new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings))
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices((context, services) =>
                    {
                        services.AddDbContext<ApplicationDbContext>(options =>
                            options.UseInMemoryDatabase(databaseName));
                        services.AddIdentityCore<User>(options => options.User.RequireUniqueEmail = true)
                            .AddRoles<IdentityRole<long>>()
                            .AddEntityFrameworkStores<ApplicationDbContext>();
                        services.AddControllers().AddApplicationPart(typeof(AccountController).Assembly);
                        services.AddApiVersioning(options =>
                        {
                            options.AssumeDefaultVersionWhenUnspecified = true;
                            options.DefaultApiVersion = new ApiVersion(1, 0);
                            options.ApiVersionReader = new UrlSegmentApiVersionReader();
                        });
                        services.AddMediatR(configuration =>
                            configuration.RegisterServicesFromAssembly(typeof(DevelopmentLoginCommandHandler).Assembly));
                        services.Configure<DevelopmentAuthenticationOptions>(
                            context.Configuration.GetSection(DevelopmentAuthenticationOptions.SectionName));
                        services.AddSingleton<IDevelopmentAuthenticationGuard, DevelopmentAuthenticationGuard>();
                        services.AddScoped<IUserService, UserService>();
                        services.AddScoped<IPlayerRepository, PlayerRepository>();
                        services.AddScoped<IExternalPlayerLoginWorkflow, ExternalPlayerLoginWorkflow>();
                        services.AddScoped(_ => Mock.Of<ICommunityLoginActivationService>());
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateIssuerSigningKey = true,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero,
                                ValidIssuer = settings["JWTOptions:Issuer"],
                                ValidAudience = settings["JWTOptions:Audience"],
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                            });
                        services.AddAuthorization();
                    });
                    webHost.Configure(app =>
                    {
                        app.UseMiddleware<GlobalExceptionHandler>();
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => endpoints.MapControllers());
                    });
                })
                .StartAsync();

            return new TestApplication(host);
        }

        public async Task<IReadOnlyList<DevelopmentPlayerSeeder.SeededPlayer>> SeedAsync()
        {
            using var scope = _host.Services.CreateScope();
            var guard = scope.ServiceProvider.GetRequiredService<IDevelopmentAuthenticationGuard>();
            guard.CanSeed.Should().BeTrue();
            return await DevelopmentPlayerSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                scope.ServiceProvider.GetRequiredService<UserManager<User>>());
        }

        public async Task<BaseResponse<LoginResponse>> LoginAsync(string accountKey)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/v1/Account/development-login",
                new DevelopmentLoginRequest { AccountKey = accountKey });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<BaseResponse<LoginResponse>>())!;
        }

        public async Task<int> CountUsersAsync()
        {
            using var scope = _host.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<UserManager<User>>().Users.CountAsync();
        }

        public async Task<int> CountPlayersAsync()
        {
            using var scope = _host.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Players.CountAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
