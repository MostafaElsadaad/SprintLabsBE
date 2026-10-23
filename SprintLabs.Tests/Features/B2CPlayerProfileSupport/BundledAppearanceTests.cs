using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Controllers;
using API.Exceptions;
using Application;
using Asp.Versioning;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using FluentAssertions;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Responses;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class BundledAppearanceTests
{
    private const string Avatar = "Assets/_Project/Backend_Data/Avatar/Set A/avatar.png";
    private const string Picture = "Assets/_Project/Backend_Data/ProfilePic/portrait.jpg";

    [Fact]
    public async Task Appearance_save_does_not_overwrite_progression_awarded_after_profile_was_loaded()
    {
        using var fixture = new Fixture(); await fixture.SeedAsync();
        using var editing = fixture.Context();
        var repository = new PlayerRepository(editing);
        var player = (await repository.GetByUserIdAsync(10))!;
        player.AvatarAssetPath = Avatar;
        using (var awarding = fixture.Context())
        {
            await awarding.Players.Where(x => x.Id == 100).ExecuteUpdateAsync(x => x
                .SetProperty(p => p.Gold, 75).SetProperty(p => p.Experience, 150).SetProperty(p => p.Level, 3));
        }
        await repository.UpdatePlayer(player);
        using var verify = fixture.Context();
        var stored = await verify.Players.SingleAsync(x => x.Id == 100);
        stored.AvatarAssetPath.Should().Be(Avatar);
        stored.Gold.Should().Be(75);
        stored.Experience.Should().Be(150);
        stored.Level.Should().Be(3);
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".JPG")]
    [InlineData(".jpeg")]
    public async Task Safe_nested_paths_at_the_512_character_limit_are_stored_exactly(string extension)
    {
        using var fixture = new Fixture(); await fixture.SeedAsync();
        using var client = fixture.Server.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "10");
        const string prefix = "Assets/_Project/Backend_Data/Avatar/Set A/";
        var path = prefix + new string('a', 512 - prefix.Length - extension.Length) + extension;
        (await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", new { avatarAssetPath = path })).EnsureSuccessStatusCode();
        var read = (await client.GetFromJsonAsync<JsonElement>("/api/v1/Users/me/player-profile")).GetProperty("data");
        read.GetProperty("avatarAssetPath").GetString().Should().Be(path);
        (await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", new { avatarAssetPath = prefix + "a" + path[prefix.Length..] })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Appearance_roundtrip_survives_new_context_and_ignores_spoofed_identity_and_progression()
    {
        using var fixture = new Fixture();
        await fixture.SeedAsync();
        using var client = fixture.Server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "10");
        var response = await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", new
        {
            avatarAssetPath = Avatar, profilePictureAssetPath = Picture,
            userId = 11, playerProfileId = 200, gold = 999, experience = 999
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        saved.GetProperty("avatarAssetPath").GetString().Should().Be(Avatar);
        saved.GetProperty("profilePictureAssetPath").GetString().Should().Be(Picture);

        var read = (await client.GetFromJsonAsync<JsonElement>("/api/v1/Users/me/player-profile?userId=11")).GetProperty("data");
        read.GetProperty("avatarAssetPath").GetString().Should().Be(Avatar);
        read.GetProperty("profilePictureAssetPath").GetString().Should().Be(Picture);
        read.GetProperty("pictureUrl").GetString().Should().Be("legacy.png");
        read.GetProperty("gold").GetInt32().Should().Be(5);
        read.GetProperty("experience").GetInt32().Should().Be(50);
        using var verify = fixture.Context();
        var stored = JsonSerializer.SerializeToElement(await verify.Players.SingleAsync(x => x.Id == 100));
        stored.GetProperty("AvatarAssetPath").GetString().Should().Be(Avatar);
        stored.GetProperty("ProfilePictureAssetPath").GetString().Should().Be(Picture);
        var other = JsonSerializer.SerializeToElement(await verify.Players.SingleAsync(x => x.Id == 200));
        other.GetProperty("AvatarAssetPath").ValueKind.Should().Be(JsonValueKind.Null);
        other.GetProperty("ProfilePictureAssetPath").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Patch_and_post_preserve_omitted_choices_and_clear_only_explicit_nulls()
    {
        using var fixture = new Fixture(); await fixture.SeedAsync();
        using var client = fixture.Server.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "10");
        (await client.PatchAsJsonAsync("/api/v1/PlayerProfiles/me", new { avatarAssetPath = Avatar, profilePictureAssetPath = Picture })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/player-profiles/me/appearance", new { profilePictureAssetPath = (string?)null })).EnsureSuccessStatusCode();
        var read = (await client.GetFromJsonAsync<JsonElement>("/api/v1/Users/me/player-profile")).GetProperty("data");
        read.GetProperty("avatarAssetPath").GetString().Should().Be(Avatar);
        read.GetProperty("profilePictureAssetPath").ValueKind.Should().Be(JsonValueKind.Null);
        (await client.PatchAsJsonAsync("/api/v1/PlayerProfiles/me", new { avatarAssetPath = (string?)null })).EnsureSuccessStatusCode();
        read = (await client.GetFromJsonAsync<JsonElement>("/api/v1/Users/me/player-profile")).GetProperty("data");
        read.GetProperty("avatarAssetPath").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("Assets/_Project/Backend_Data/ProfilePic/wrong.png")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/../ProfilePic/a.png")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/./a.png")]
    [InlineData("Assets/_Project/Backend_Data/Avatar//a.png")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/a.gif")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/a.png?x=1")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/%2e%2e/a.png")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/a\\b.png")]
    [InlineData("assets/_Project/Backend_Data/Avatar/a.png")]
    [InlineData("https://example.com/a.png")]
    [InlineData("/Assets/_Project/Backend_Data/Avatar/a.png")]
    [InlineData("C:/Assets/_Project/Backend_Data/Avatar/a.png")]
    [InlineData("")]
    [InlineData("Assets/_Project/Backend_Data/Avatar/a\n.png")]
    public async Task Invalid_avatar_is_rejected_without_writing_other_fields(string path)
    {
        using var fixture = new Fixture(); await fixture.SeedAsync();
        using var client = fixture.Server.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "10");
        var response = await client.PatchAsJsonAsync("/api/v1/PlayerProfiles/me", new { avatarAssetPath = path, age = 10, profilePictureAssetPath = Picture });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var verify = fixture.Context(); (await verify.Players.SingleAsync(x => x.Id == 100)).Age.Should().Be(9);
    }

    [Fact]
    public async Task Wrong_type_wrong_picture_root_and_overlength_paths_are_rejected()
    {
        using var fixture = new Fixture(); await fixture.SeedAsync();
        using var client = fixture.Server.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-User", "10");
        foreach (var body in new object[] { new { avatarAssetPath = 42 }, new { profilePictureAssetPath = Avatar }, new { avatarAssetPath = "Assets/_Project/Backend_Data/Avatar/" + new string('a', 480) + ".png" } })
            (await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", body)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_and_suspended_users_cannot_change_appearance()
    {
        using var fixture = new Fixture(); await fixture.SeedAsync(); using var client = fixture.Server.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", new { avatarAssetPath = Avatar })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PatchAsJsonAsync("/api/v1/PlayerProfiles/me", new { avatarAssetPath = Avatar })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "12");
        (await client.PostAsJsonAsync("/api/v1/PlayerProfiles/me/appearance", new { avatarAssetPath = Avatar })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public TestServer Server { get; }
        public Fixture()
        {
            _connection.Open();
            var users = new Mock<IUserService>();
            users.Setup(x => x.GetCurrentUser(It.IsAny<long>())).ReturnsAsync((long id) => new UserIdentityResponse { Id = id, IsSuspended = id == 12 });
            Server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
            {
                services.AddLogging(x => x.ClearProviders());
                services.AddControllers().AddApplicationPart(typeof(PlayerProfilesController).Assembly);
                services.AddApiVersioning(x => { x.DefaultApiVersion = new ApiVersion(1, 0); x.ApiVersionReader = new UrlSegmentApiVersionReader(); });
                services.AddApplicationServices(new ConfigurationBuilder().Build());
                services.AddScoped<ApplicationDbContext>(_ => Context());
                services.AddScoped<IPlayerRepository, PlayerRepository>();
                services.AddSingleton(users.Object);
                services.AddAuthentication("TestPlayer").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestPlayer", _ => { });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseMiddleware<GlobalExceptionHandler>(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(x => x.MapControllers());
            }));
        }
        public ApplicationDbContext Context() => new SqliteProfileContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        public async Task SeedAsync()
        {
            using var context = Context(); await context.Database.EnsureCreatedAsync();
            context.Users.AddRange(new User { Id = 10, Name = "Current", UserName = "current@example.com", Email = "current@example.com" }, new User { Id = 11, Name = "Other", UserName = "other@example.com", Email = "other@example.com" });
            context.Players.AddRange(
                new Player { Id = 100, UserId = 10, Name = "Current", Email = "current@example.com", AvatarUrl = "legacy.png", Age = 9, Gold = 5, Experience = 50 },
                new Player { Id = 200, UserId = 11, Name = "Other", Email = "other@example.com" });
            await context.SaveChangesAsync();
        }
        public void Dispose() { Server.Dispose(); _connection.Dispose(); }
    }

    private sealed class SqliteProfileContext : ApplicationDbContext
    {
        public SqliteProfileContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, Scheme.Name)), Scheme.Name)));
        }
    }
}
