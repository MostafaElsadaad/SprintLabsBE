using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Authentication;
using API.Controllers;
using API.Exceptions;
using Application;
using Asp.Versioning;
using Domain.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compass.Tests.Features.Missions;
public class MissionApiTests
{
    [Fact]
    public async Task Event_endpoint_requires_game_server_authentication_and_rejects_player_jwt()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync();
        var credential = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var server = Server(f, credential); using var client = server.CreateClient();
        var e = f.Event();
        (await client.PostAsJsonAsync("/api/v1/game-server/missions/events", e)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "ordinary-user-token");
        (await client.PostAsJsonAsync("/api/v1/game-server/missions/events", e)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", credential);
        (await client.PostAsJsonAsync("/api/v1/game-server/missions/events", e)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/v1/game-server/missions/events/bulk", new[] { e })).StatusCode.Should().Be(HttpStatusCode.OK);
        f.Context.MissionEventLogs.Count().Should().Be(1);
    }
    [Theory] [InlineData(null)] [InlineData("short")]
    public async Task Missing_server_configuration_fails_closed(string? configured)
    {
        using var f = new MissionFixture(); await f.SeedAsync();
        using var server = Server(f, configured); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        (await client.PostAsJsonAsync("/api/v1/game-server/missions/events", f.Event())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        f.Context.MissionEventLogs.Should().BeEmpty();
    }
    [Fact]
    public async Task Player_routes_use_claimed_identity_and_admin_routes_enforce_platform_admin()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() }, default);
        using var server = Server(f, null); using var client = server.CreateClient();
        (await client.GetAsync("/api/v1/Missions/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "2");
        (await client.PostAsync($"/api/v1/Missions/{m.PlayerMissionId}/claim?userId=1", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync("/api/v1/admin/missions/seed", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "1");
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/Missions/me?userId=2")).GetProperty("data").GetArrayLength().Should().Be(1);
        (await client.PostAsync($"/api/v1/Missions/{m.PlayerMissionId}/claim", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/api/v1/Missions/claim-all", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "40");
        (await client.GetAsync("/api/v1/admin/missions/templates")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
    private static TestServer Server(MissionFixture f, string? credential) => new(new WebHostBuilder()
        .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["GameServer:ApiKey"] = credential }))
        .ConfigureServices(services =>
        {
            services.AddLogging(x => x.ClearProviders());
            services.AddControllers().AddApplicationPart(typeof(MatchesController).Assembly);
            services.AddApiVersioning(x => { x.DefaultApiVersion = new ApiVersion(1, 0); x.ApiVersionReader = new UrlSegmentApiVersionReader(); });
            services.AddApplicationServices(new ConfigurationBuilder().Build());
            services.AddSingleton<IMissionService>(f.Service);
            services.AddAuthentication("TestPlayer")
                .AddScheme<AuthenticationSchemeOptions, TestPlayerAuthenticationHandler>("TestPlayer", _ => { })
                .AddScheme<AuthenticationSchemeOptions, GameServerAuthenticationHandler>(GameServerAuthenticationHandler.SchemeName, _ => { });
            services.AddAuthorization();
        }).Configure(app =>
        {
            app.UseMiddleware<GlobalExceptionHandler>();
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(x => x.MapControllers());
        }));

    private sealed class TestPlayerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestPlayerAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, Scheme.Name)), Scheme.Name)));
        }
    }
}
