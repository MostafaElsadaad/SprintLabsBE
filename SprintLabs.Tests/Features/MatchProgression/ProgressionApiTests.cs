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

namespace Compass.Tests.Features.MatchProgression;

public class ProgressionApiTests
{
    [Fact]
    public async Task Only_game_server_credentials_can_register_or_complete_matches()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var credential = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var server = Server(f, credential); using var client = server.CreateClient();
        var registration = f.Registration();
        (await client.PostAsJsonAsync("/api/v1/Matches", registration)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "ordinary-user-token");
        (await client.PostAsJsonAsync("/api/v1/Matches", registration)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        (await client.PostAsJsonAsync("/api/v1/Matches", registration)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("gameserver", credential);
        var response = await client.PostAsJsonAsync("/api/v1/Matches", registration);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("matchId").GetInt64();
        var result = await client.PostAsJsonAsync($"/api/v1/Matches/{id}/complete", f.Completion());
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var original = await result.Content.ReadAsStringAsync();
        var retry = await client.PostAsJsonAsync($"/api/v1/Matches/{id}/complete", f.Completion());
        (await retry.Content.ReadAsStringAsync()).Should().Be(original);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.PostAsJsonAsync($"/api/v1/Matches/{id}/complete", f.Completion())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        f.Context.Players.Single(x => x.Id == 101).TotalMatches.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public async Task Missing_or_weak_server_configuration_fails_closed(string? configured)
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        using var server = Server(f, configured); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        (await client.PostAsJsonAsync("/api/v1/Matches", f.Registration())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        f.Context.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_routes_take_caller_identity_from_authenticated_claims()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        using var server = Server(f, null); using var client = server.CreateClient();
        (await client.GetAsync("/api/v1/Progression/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        var self = await client.GetFromJsonAsync<JsonElement>("/api/v1/Progression/me?userId=2&playerProfileId=102");
        self.GetProperty("data").GetProperty("playerProfileId").GetInt64().Should().Be(101);
        (await client.GetAsync("/api/v1/Progression/players/102")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/v1/Ranking/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/Ranking/leaderboard?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/v1/Matches/me/history")).StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "20");
        var board = await client.GetFromJsonAsync<JsonElement>("/api/v1/Communities/1/ranking/leaderboard");
        board.GetProperty("data").GetProperty("total").GetInt32().Should().Be(1);
        (await client.GetAsync("/api/v1/Ranking/players/102")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/v1/Communities/1/matches")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/Communities/1/players/102/matches")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Leaderboard_lists_and_own_routes_preserve_envelopes_and_ignore_spoofed_identity()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        using var server = Server(f, null); using var client = server.CreateClient();
        (await client.GetAsync("/api/v1/Ranking/leaderboard/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/Ranking/leaderboard?pageSize=1");
        var data = list.GetProperty("data");
        data.GetProperty("items")[0].GetProperty("points").GetInt64().Should().Be(1000);
        data.GetProperty("currentPlayer").GetProperty("position").GetInt64().Should().Be(3);
        data.GetProperty("period").GetString().Should().Be("AllTime");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/Ranking/leaderboard/me?userId=2&playerProfileId=102&page=99");
        me.GetProperty("data").GetProperty("currentPlayer").GetProperty("playerProfileId").GetInt64().Should().Be(101);
        var school = await client.GetFromJsonAsync<JsonElement>("/api/v1/Communities/1/ranking/leaderboard/me?classId=1");
        school.GetProperty("data").GetProperty("currentPlayer").GetProperty("position").GetInt64().Should().Be(1);
        (await client.GetAsync("/api/v1/Communities/1/ranking/leaderboard?classId=2")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/Communities/2/ranking/leaderboard/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/Ranking/leaderboard?classId=1")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/v1/Ranking/leaderboard/me?period=Daily")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync("/api/v1/Ranking/leaderboard?period=1")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Period_leaderboard_http_returns_ranked_activity_and_utc_boundaries()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(), default);
        await f.Matches.CompleteAsync(match.MatchId, f.Completion(), default);
        using var server = Server(f, null); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        var result = await client.GetFromJsonAsync<JsonElement>("/api/v1/Ranking/leaderboard?period=month");
        var data = result.GetProperty("data");
        data.GetProperty("period").GetString().Should().Be("Month");
        data.GetProperty("periodStartsAt").GetString().Should().EndWith("Z");
        data.GetProperty("periodEndsAt").GetString().Should().EndWith("Z");
        data.GetProperty("total").GetInt32().Should().Be(2);
        data.GetProperty("currentPlayer").GetProperty("points").GetInt64().Should().Be(30);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "3");
        var unranked = await client.GetFromJsonAsync<JsonElement>("/api/v1/Ranking/leaderboard/me?period=Month");
        unranked.GetProperty("data").GetProperty("currentPlayer").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static TestServer Server(ProgressionFixture f, string? credential) => new(new WebHostBuilder()
        .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["GameServer:ApiKey"] = credential }))
        .ConfigureServices(services =>
        {
            services.AddLogging(x => x.ClearProviders());
            services.AddControllers().AddApplicationPart(typeof(MatchesController).Assembly);
            services.AddApiVersioning(x => { x.DefaultApiVersion = new ApiVersion(1, 0); x.ApiVersionReader = new UrlSegmentApiVersionReader(); });
            services.AddApplicationServices(new ConfigurationBuilder().Build());
            services.AddSingleton<IMatchProgressionService>(f.Matches);
            services.AddSingleton<IProgressionReadService>(f.Reads);
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
