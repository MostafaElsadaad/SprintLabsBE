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
using Infrastructure.DataAccess;
using Infrastructure.Services;
using Infrastructure.Repositories;
using Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataApiTests
{
    [Fact]
    public async Task Existing_get_returns_new_bank_in_unity_format_without_authentication()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("TrueOrFalse"); q.TimerSeconds = 7.25m;
        await f.Service.PublishAsync(40, q, default);
        var credential = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var server = Server(f, credential); using var client = server.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Question?grade=7")).StatusCode);
        var result = await client.GetFromJsonAsync<JsonElement>("/api/v1/Question?grade=7&unit=1");
        var first = result.GetProperty("data").GetProperty("payloadJson").GetProperty("Questions")[0];
        Assert.Equal(q.QuestionId.ToString(), first.GetProperty("QuestionId").GetString());
        Assert.Equal(7.25m, first.GetProperty("TrueOrFalse").GetProperty("Timer").GetDecimal());
        Assert.False(first.GetProperty("TrueOrFalse").GetProperty("CorrectAnswer").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/Question?grade=7&unit=2")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", "invalid-credential");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Question?grade=7&unit=1")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", credential);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/Question/history", new
            { playerId = 101, questionId = q.QuestionId, matchId = 10, selectedAnswer = false, timeTakenMs = 4200 })).StatusCode);
    }

    [Fact]
    public async Task Admin_can_convert_legacy_pack_and_existing_get_keeps_unity_payload_shape()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        using var server = Server(f, null); using var client = server.CreateClient();
        var old = await client.GetFromJsonAsync<JsonElement>("/api/v1/Question?grade=5&assignment=1");
        Assert.Equal(5, old.GetProperty("data").GetProperty("payloadJson").GetProperty("Questions").GetArrayLength());
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/Question/bank/import-legacy/1", null)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "40");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/Question/bank/import-legacy/1", null)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        var converted = await client.GetFromJsonAsync<JsonElement>("/api/v1/Question?grade=5&assignment=1");
        var data = converted.GetProperty("data");
        Assert.Equal(old.GetProperty("data").GetProperty("id").GetInt64(), data.GetProperty("id").GetInt64());
        Assert.Equal(old.GetProperty("data").GetProperty("version").GetInt32(), data.GetProperty("version").GetInt32());
        var first = data.GetProperty("payloadJson").GetProperty("Questions")[0];
        Assert.True(Guid.TryParse(first.GetProperty("QuestionId").GetString(), out _));
        Assert.Equal(8m, first.GetProperty("MCQ").GetProperty("Timer").GetDecimal());
    }

    [Fact]
    public async Task Server_reads_and_history_writes_reject_player_credentials_and_accept_configured_server()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("TrueOrFalse"); await f.Service.PublishAsync(40, q, default);
        var credential = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var server = Server(f, credential); using var client = server.CreateClient();
        var body = QuestionDataTypesTests.Response(q);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/Question/history", body)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/Question/bank/server?grade=7")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/Question/history", body)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GameServer", credential);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Question/bank/server?grade=7")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/Question/history", body)).StatusCode);
        var export = await client.GetFromJsonAsync<JsonElement>("/api/v1/Question/history/server/players/101");
        Assert.False(export.GetProperty("data").GetProperty("items")[0].GetProperty("selectedAnswer").GetBoolean());
        var historyId = export.GetProperty("data").GetProperty("items")[0].GetProperty("historyId").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/Question/history/{historyId}", body)).StatusCode);
        Assert.Single((await f.Service.HistoryAsync(null, 101, null, 1, 50, default)).Items);
        body.SelectedAnswer = true;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/v1/Question/history/{historyId}", body)).StatusCode);
    }

    [Fact]
    public async Task Display_omits_answer_keys_and_correct_matching_associations()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        foreach (var type in new[] { "TrueOrFalse", "FillBlank", "Ordering", "MatchingPairs", "DragAndDrop" })
            await f.Service.PublishAsync(40, QuestionDataTypesTests.Question(type), default);
        using var server = Server(f, null); using var client = server.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/Question/bank?grade=7")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        var response = await client.GetAsync("/api/v1/Question/bank?grade=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        foreach (var hidden in new[] { "correctAnswer", "acceptedAnswers", "correctPosition", "isCorrect", "pairId", "blankAnswers" })
            Assert.DoesNotContain(hidden, text);
        using var parsed = JsonDocument.Parse(text);
        var matching = parsed.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().Single(x => x.GetProperty("questionType").GetString() == "MatchingPairs");
        Assert.Equal("left-0", matching.GetProperty("matchingLeft")[0].GetProperty("id").GetString());
        Assert.Equal("right-0", matching.GetProperty("matchingRight")[0].GetProperty("id").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/Question/bank/admin?grade=7")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/Question/bank?grade=7&pageSize=201")).StatusCode);
    }

    [Fact]
    public async Task Publishing_requires_admin_and_player_history_cannot_be_spoofed()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("TrueOrFalse");
        using var server = Server(f, null); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "1");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/v1/Question/bank", q)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "40");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/Question/bank", q)).StatusCode);
        await f.Service.RecordAsync(QuestionDataTypesTests.Response(q), default);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "2");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/Question/history/players/101?userId=1")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "1");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Question/history/players/101")).StatusCode);
    }

    private static TestServer Server(QuestionDataServiceTests.Fixture f, string? credential) => new(new WebHostBuilder()
        .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["GameServer:ApiKey"] = credential, ["JWTOptions:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) }))
        .ConfigureServices(services =>
        {
            services.AddLogging(x => x.ClearProviders());
            services.AddControllers().AddApplicationPart(typeof(QuestionDataController).Assembly);
            services.AddApiVersioning(x => { x.DefaultApiVersion = new ApiVersion(1, 0); x.ApiVersionReader = new UrlSegmentApiVersionReader(); });
            services.AddApplicationServices(new ConfigurationBuilder().Build());
            services.AddSingleton<ApplicationDbContext>(f.Context);
            services.AddIdentityCore<User>().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<IQuestionDataService>(f.Service);
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddAuthentication("TestPlayer").AddScheme<AuthenticationSchemeOptions, TestPlayerHandler>("TestPlayer", _ => { })
                .AddScheme<AuthenticationSchemeOptions, GameServerAuthenticationHandler>(GameServerAuthenticationHandler.SchemeName, _ => { });
            services.AddAuthorization();
        }).Configure(app =>
        {
            app.UseMiddleware<GlobalExceptionHandler>(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(x => x.MapControllers());
        }));

    private sealed class TestPlayerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, Scheme.Name)), Scheme.Name)));
        }
    }
}
