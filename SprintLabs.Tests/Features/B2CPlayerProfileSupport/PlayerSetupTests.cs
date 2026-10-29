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

public class PlayerSetupTests
{
    private const string Route = "/api/v1/PlayerProfiles/me/setup";
    private static object Valid(string nickname = "Space Explorer", int grade = 7) => new
        { nickname, grade, curriculum = "Egyptian National", language = "English" };

    [Fact]
    public async Task First_login_requires_setup_then_save_roundtrips_without_changing_account_or_wallet()
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-User", "10");
        var initial = (await c.GetFromJsonAsync<JsonElement>(Route)).GetProperty("data");
        initial.GetProperty("requiresSetup").GetBoolean().Should().BeTrue();
        (await c.PostAsJsonAsync(Route, Valid())).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(Route, Valid())).EnsureSuccessStatusCode();
        var saved = (await c.GetFromJsonAsync<JsonElement>(Route)).GetProperty("data");
        saved.GetProperty("requiresSetup").GetBoolean().Should().BeFalse();
        saved.GetProperty("nickname").GetString().Should().Be("Space Explorer");
        saved.GetProperty("grade").GetInt32().Should().Be(7);
        saved.GetProperty("curriculum").GetString().Should().Be("Egyptian National");
        saved.GetProperty("language").GetString().Should().Be("English");
        var profile = (await c.GetFromJsonAsync<JsonElement>("/api/v1/Users/me/player-profile")).GetProperty("data");
        profile.GetProperty("name").GetString().Should().Be("Space Explorer");
        profile.GetProperty("curriculum").GetString().Should().Be("Egyptian National");
        using var db = f.Context();
        (await db.Players.SingleAsync(x => x.Id == 100)).Gold.Should().Be(5);
        (await db.Players.SingleAsync(x => x.Id == 100)).Experience.Should().Be(50);
        (await db.Users.SingleAsync(x => x.Id == 10)).Name.Should().Be("Current");
        (await db.Players.SingleAsync(x => x.Id == 200)).Name.Should().Be("Other");
    }

    [Theory]
    [InlineData(6)] [InlineData(13)] [InlineData(0)]
    public async Task Unsupported_grade_is_rejected_without_partial_write(int grade)
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        (await c.PostAsJsonAsync(Route, Valid(grade: grade))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var db = f.Context(); (await db.Players.SingleAsync(x => x.Id == 100)).Name.Should().Be("Current");
    }

    [Theory]
    [InlineData("", "Egyptian National", "English")]
    [InlineData("Test", "Unknown", "English")]
    [InlineData("Test", "IG", "Arabic")]
    [InlineData("Test", "Egyptian National", "Unknown")]
    public async Task Invalid_or_unlisted_values_are_rejected(string nickname, string curriculum, string language)
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        (await c.PostAsJsonAsync(Route, new { nickname, grade = 7, curriculum, language })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Options_include_seeded_examples_and_question_bank_pairs_and_all_six_grades()
    {
        using var f = new Fixture(); await f.SeedAsync();
        using (var db = f.Context())
        {
            db.Set<Domain.Models.QuestionData.Question>().Add(new() { QuestionId = Guid.NewGuid(), Curriculum = "Book Curriculum", Language = "French", Grade = 7, Subject = "Science", Unit = 1, Lesson = 1, Term = 1, QuestionText = "Test", QuestionType = Domain.Enums.QuestionType.MCQ });
            await db.SaveChangesAsync();
        }
        using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        var data = (await c.GetFromJsonAsync<JsonElement>("/api/v1/PlayerProfiles/setup-options")).GetProperty("data");
        data.GetProperty("grades").EnumerateArray().Select(x => x.GetInt32()).Should().Equal(7,8,9,10,11,12);
        var pairs = data.GetProperty("educationOptions").EnumerateArray().ToArray();
        pairs.Should().Contain(x => x.GetProperty("curriculum").GetString() == "Egyptian National");
        pairs.Should().Contain(x => x.GetProperty("curriculum").GetString() == "Book Curriculum" && x.GetProperty("language").GetString() == "French");
        (await c.PostAsJsonAsync(Route, new { nickname = "Livre", grade = 12, curriculum = "Book Curriculum", language = "French" })).EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Community_student_skips_setup_and_cannot_self_change_education(bool pending)
    {
        using var f = new Fixture(); await f.SeedAsync();
        using (var db = f.Context())
        {
            db.Communities.Add(new Community { Id = 1, Name = "School", Slug = "school" });
            db.Grades.Add(new Grade { Id = 1, CommunityId = 1, Name = "Grade 8", Value = 8 });
            db.Classes.Add(new Class { Id = 1, CommunityId = 1, GradeId = 1, Name = "A" });
            db.StudentLicenses.Add(new StudentLicense { Id = 1, CommunityId = 1, GradeId = 1, ClassId = 1, AssignedByUserId = 10, UserId = pending ? null : 10, PlayerProfileId = pending ? null : 100, Email = "current@example.com", Status = pending ? Domain.Enums.StudentLicenseStatus.Pending : Domain.Enums.StudentLicenseStatus.Active });
            await db.SaveChangesAsync();
        }
        using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        var data = (await c.GetFromJsonAsync<JsonElement>(Route)).GetProperty("data");
        data.GetProperty("isCommunityManaged").GetBoolean().Should().BeTrue();
        data.GetProperty("requiresSetup").GetBoolean().Should().BeFalse();
        (await c.PostAsJsonAsync(Route, Valid())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var verify = f.Context(); (await verify.Players.SingleAsync(x => x.Id == 100)).Name.Should().Be("Current");
    }

    [Fact]
    public async Task Anonymous_suspended_and_missing_profiles_are_denied()
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient();
        (await c.GetAsync(Route)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.GetAsync("/api/v1/PlayerProfiles/setup-options")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsJsonAsync(Route, Valid())).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        c.DefaultRequestHeaders.Add("X-Test-User", "12");
        (await c.GetAsync(Route)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.PostAsJsonAsync(Route, Valid())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        c.DefaultRequestHeaders.Remove("X-Test-User"); c.DefaultRequestHeaders.Add("X-Test-User", "999");
        (await c.GetAsync(Route)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"nickname\":\"Test\",\"grade\":7,\"curriculum\":null,\"language\":\"English\"}")]
    [InlineData("{\"nickname\":42,\"grade\":7,\"curriculum\":\"IG\",\"language\":\"English\"}")]
    public async Task Missing_or_wrongly_typed_fields_return_bad_request(string json)
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        (await c.PostAsync(Route, body)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Caller_identity_is_authoritative_and_nickname_is_trimmed_without_wallet_write()
    {
        using var f = new Fixture(); await f.SeedAsync(); using var c = f.Server.CreateClient(); c.DefaultRequestHeaders.Add("X-Test-User", "10");
        (await c.PostAsJsonAsync(Route, new { nickname = "  Star  ", grade = 12, curriculum = " IG ", language = "English", userId = 11, playerProfileId = 200, gold = 99999 })).EnsureSuccessStatusCode();
        using var db = f.Context();
        (await db.Players.SingleAsync(x => x.Id == 100)).Name.Should().Be("Star");
        (await db.Players.SingleAsync(x => x.Id == 100)).Gold.Should().Be(5);
        (await db.Players.SingleAsync(x => x.Id == 200)).Name.Should().Be("Other");
        (await c.PostAsJsonAsync(Route, Valid(new string('a', 101)))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsJsonAsync(Route, Valid("Bad\nName"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
                services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
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
