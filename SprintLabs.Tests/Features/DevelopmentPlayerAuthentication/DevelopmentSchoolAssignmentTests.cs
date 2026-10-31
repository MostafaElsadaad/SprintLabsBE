using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Controllers;
using API.Exceptions;
using Application;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
using Compass.Tests.Features.OwnerStudentLicenseManagement;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using FluentAssertions;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Shared.DevelopmentAuthentication;
using Shared.Exceptions;
using Shared.Options;
using Shared.Responses;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentSchoolAssignmentTests
{
    [Fact]
    public async Task Assignment_and_retry_preserve_identity_progression_and_consume_one_seat()
    {
        await using var f = await Fixture.Create();
        var first = await f.Handler().Handle(f.Command(), default);
        var retry = await f.Handler().Handle(f.Command(), default);
        first.AlreadyAssigned.Should().BeFalse(); retry.AlreadyAssigned.Should().BeTrue();
        retry.StudentLicenseId.Should().Be(first.StudentLicenseId);
        first.PlayerProfileId.Should().Be(5000000000L);
        var license = await f.Context.StudentLicenses.SingleAsync();
        license.UserId.Should().Be(20); license.PlayerProfileId.Should().Be(5000000000L);
        license.Status.Should().Be(StudentLicenseStatus.Active);
        license.GradeId.Should().Be(1); license.ClassId.Should().Be(1);
        (await f.Context.CommunityLicenses.SingleAsync()).UsedStudents.Should().Be(1);
        var membership = await f.Context.CommunityUsers.SingleAsync(m => m.UserId == 20);
        membership.Role.Should().Be(CommunityUserRole.Student); membership.Status.Should().Be(CommunityUserStatus.Active);
        var player = await f.Context.Players.SingleAsync();
        player.Gold.Should().Be(123); player.Experience.Should().Be(456);
        player.GoogleId.Should().BeNull(); player.UserId.Should().Be(20);
        (await f.Context.Users.SingleAsync(u => u.Id == 20)).FirebaseUid.Should().BeNull();
    }

    [Fact]
    public async Task Pending_license_activates_without_consuming_another_seat()
    {
        await using var f = await Fixture.Create();
        var capacity = await f.Context.CommunityLicenses.SingleAsync(); capacity.MaxStudents = capacity.UsedStudents = 1;
        f.Context.StudentLicenses.Add(new StudentLicense { CommunityId = 1, Email = f.Definition.Email,
            GradeId = 1, ClassId = 1, AssignedByUserId = 10, Status = StudentLicenseStatus.Pending });
        await f.Context.SaveChangesAsync();
        await f.Handler().Handle(f.Command(), default);
        (await f.Context.StudentLicenses.SingleAsync()).Status.Should().Be(StudentLicenseStatus.Active);
        (await f.Context.CommunityLicenses.SingleAsync()).UsedStudents.Should().Be(1);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("suspended-admin")]
    [InlineData("locked-admin")]
    [InlineData("capacity")]
    [InlineData("school")]
    [InlineData("class")]
    [InlineData("foreign-class")]
    [InlineData("real-user")]
    [InlineData("firebase")]
    [InlineData("mismatched-profile")]
    [InlineData("privileged-target")]
    [InlineData("owner-membership")]
    public async Task Invalid_scope_identity_access_or_capacity_is_rejected_without_partial_enrollment(string reason)
    {
        await using var f = await Fixture.Create();
        if (reason == "admin") f.Admin.IsPlatformAdmin = false;
        if (reason == "suspended-admin") f.Admin.IsSuspended = true;
        if (reason == "locked-admin") f.Admin.IsLockedOut = true;
        if (reason == "capacity") (await f.Context.CommunityLicenses.SingleAsync()).MaxStudents = 0;
        if (reason == "school") (await f.Context.Communities.SingleAsync(c => c.Id == 1)).Status = CommunityStatus.Suspended;
        if (reason == "class") (await f.Context.Classes.SingleAsync(c => c.Id == 1)).Status = ClassStatus.Deleted;
        if (reason == "real-user") f.Target.GoogleId = "not-a-development-identity";
        if (reason == "firebase") f.Target.FirebaseUid = "not-a-development-identity";
        if (reason == "mismatched-profile") f.Target.PlayerProfileId = 999;
        if (reason == "privileged-target") f.Target.IsTeacherAccount = true;
        if (reason == "owner-membership") f.Context.CommunityUsers.Add(new CommunityUser { CommunityId = 1, UserId = 20, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        await f.Context.SaveChangesAsync();
        var command = f.Command(); if (reason == "foreign-class") command.ClassId = 3;
        await ((Func<Task>)(async () => await f.Handler().Handle(command, default))).Should().ThrowAsync<GenericException>();
        (await f.Context.StudentLicenses.CountAsync()).Should().Be(0);
        (await f.Context.CommunityLicenses.SingleAsync()).UsedStudents.Should().Be(0);
        (await f.Context.CommunityUsers.CountAsync(m => m.UserId == 20 && m.Role == CommunityUserRole.Student)).Should().Be(0);
    }

    [Fact]
    public async Task Conflicting_existing_assignment_is_not_moved()
    {
        await using var f = await Fixture.Create();
        await f.Handler().Handle(f.Command(), default);
        var changed = f.Command(); changed.GradeId = 2; changed.ClassId = 2;
        var action = () => f.Handler().Handle(changed, default);
        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await f.Context.StudentLicenses.SingleAsync()).ClassId.Should().Be(1);
        (await f.Context.CommunityLicenses.SingleAsync()).UsedStudents.Should().Be(1);
    }

    [Theory]
    [InlineData("Production", true, false, 404)]
    [InlineData("Staging", false, false, 404)]
    [InlineData("Staging", true, true, 401)]
    public async Task Development_guard_blocks_before_account_lookup(string environment, bool enabled, bool requireKey, int code)
    {
        await using var f = await Fixture.Create();
        var host = new Mock<IHostEnvironment>(); host.SetupGet(h => h.EnvironmentName).Returns(environment);
        var guard = new DevelopmentAuthenticationGuard(host.Object, Options.Create(new DevelopmentAuthenticationOptions
        { Enabled = enabled, ApiKey = requireKey ? "unit-test-guard" : null }));
        var action = () => f.Handler(guard).Handle(f.Command(), default);
        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be((HttpStatusCode)code);
        f.Users.Verify(u => u.FindByEmail(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_account_is_not_created()
    {
        await using var f = await Fixture.Create(); var command = f.Command(); command.AccountKey = "ordinary-user";
        var action = () => f.Handler().Handle(command, default);
        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await f.Context.Players.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("Staging", 200)]
    [InlineData("Production", 404)]
    public async Task Http_route_requires_admin_and_development_key_and_never_accepts_body_identity(string environment, int expected)
    {
        await using var f = await Fixture.Create();
        f.Users.Setup(u => u.GetCurrentUser(20)).ReturnsAsync(() => f.Target);
        var host = new Mock<IHostEnvironment>(); host.SetupGet(h => h.EnvironmentName).Returns(environment);
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddLogging(l => l.ClearProviders());
            services.AddControllers().AddApplicationPart(typeof(AdminDevelopmentPlayersController).Assembly);
            services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.ApiVersionReader = new UrlSegmentApiVersionReader(); });
            services.AddApplicationServices(new ConfigurationBuilder().Build());
            services.AddScoped<ApplicationDbContext>(_ => f.OpenContext());
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddSingleton(f.Users.Object); services.AddSingleton(f.PlayerRepository);
            services.AddScoped<IStudentEnrollmentTransaction, StudentEnrollmentTransaction>();
            services.AddSingleton<IDevelopmentAuthenticationGuard>(new DevelopmentAuthenticationGuard(host.Object,
                Options.Create(new DevelopmentAuthenticationOptions { Enabled = true, ApiKey = "unit-test-guard" })));
            services.AddAuthentication("TestUser").AddScheme<AuthenticationSchemeOptions, TestUserHandler>("TestUser", _ => { });
            services.AddAuthorization();
        }).Configure(app =>
        {
            app.UseMiddleware<GlobalExceptionHandler>(); app.UseRouting(); app.UseAuthentication();
            app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers());
        }));
        using var client = server.CreateClient();
        const string route = "/api/v1/admin/development-players/dev-player-01/school";
        var body = new { communityId = 1, gradeId = 1, classId = 1, userId = 999, playerProfileId = 888 };
        (await client.PostAsJsonAsync(route, body)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "40");
        var noKey = await client.PostAsJsonAsync(route, body);
        noKey.StatusCode.Should().Be(environment == "Production" ? HttpStatusCode.NotFound : HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add(DevelopmentAuthenticationOptions.HeaderName, "unit-test-guard");
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "20");
        (await client.PostAsJsonAsync(route, body)).StatusCode.Should().Be(environment == "Production" ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Remove("X-Test-User"); client.DefaultRequestHeaders.Add("X-Test-User", "40");
        var response = await client.PostAsJsonAsync(route, body);
        ((int)response.StatusCode).Should().Be(expected);
        if (expected == 200)
        {
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            data.GetProperty("playerProfileId").GetInt64().Should().Be(5000000000L);
            (await f.Context.StudentLicenses.SingleAsync()).UserId.Should().Be(20);
        }
        else (await f.Context.StudentLicenses.CountAsync()).Should().Be(0);
    }

    private sealed class TestUserHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
    [Theory]
    [InlineData(5, true)]
    [InlineData(-5, false)]
    public async Task Persisted_admin_lockout_is_enforced_through_real_user_service(int minutes, bool locked)
    {
        await using var f = await Fixture.Create();
        var admin = await f.Context.Users.SingleAsync(u => u.Id == 40);
        admin.LockoutEnabled = true; admin.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(minutes);
        await f.Context.SaveChangesAsync();
        using var userStore = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(f.Context);
        using var manager = new UserManager<User>(userStore, Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(), new IUserValidator<User>[] { new UserValidator<User>() },
            Array.Empty<IPasswordValidator<User>>(), new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<User>>.Instance);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["JWTOptions:Secret"] = new string('x', 64), ["JWTOptions:Issuer"] = "test", ["JWTOptions:Audience"] = "test" }).Build();
        var realUsers = new UserService(configuration, manager);
        (await realUsers.GetCurrentUser(40))!.IsLockedOut.Should().Be(locked);
        if (locked)
        {
            var action = () => f.Handler(userService: realUsers).Handle(f.Command(), default);
            (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await f.Context.StudentLicenses.CountAsync()).Should().Be(0);
        }
        else
        {
            await f.Handler(userService: realUsers).Handle(f.Command(), default);
            (await f.Context.StudentLicenses.CountAsync()).Should().Be(1);
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public ApplicationDbContext Context { get; private set; } = null!;
        public Mock<IUserService> Users { get; } = new();
        private readonly Mock<IPlayerRepository> players = new();
        public DevelopmentPlayerCatalog.Entry Definition { get; } = DevelopmentPlayerCatalog.All[0];
        public UserIdentityResponse Admin { get; } = new() { Id = 40, Status = "Active", IsPlatformAdmin = true };
        public UserIdentityResponse Target { get; private set; } = null!;
        public static async Task<Fixture> Create()
        {
            var f = new Fixture(); await f.connection.OpenAsync();
            f.Context = new SqliteContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(f.connection).Options);
            await f.Context.Database.EnsureCreatedAsync();
            f.Context.Users.AddRange(new User { Id = 10, Name = "Owner", UserName = "owner-test", Email = "owner@example.test" },
                new User { Id = 40, Name = "Admin", UserName = "admin-test", Email = "admin@example.test", IsPlatformAdmin = true },
                new User { Id = 20, Name = f.Definition.DisplayName, UserName = f.Definition.AccountKey, Email = f.Definition.Email });
            await OwnerStudentLicenseManagementTestHelper.SeedBase(f.Context);
            f.Context.Players.Add(new Player { Id = 5000000000L, UserId = 20, Name = "Renamed test player", Email = f.Definition.Email, Gold = 123, Experience = 456 });
            await f.Context.SaveChangesAsync();
            f.Target = new UserIdentityResponse { Id = 20, UserName = f.Definition.AccountKey, Email = f.Definition.Email, Status = "Active", PlayerProfileId = 5000000000L };
            f.Users.Setup(u => u.GetCurrentUser(40)).ReturnsAsync(() => f.Admin);
            f.Users.Setup(u => u.FindByEmail(f.Definition.Email)).ReturnsAsync(() => f.Target);
            f.players.Setup(p => p.GetByUserIdAsync(20)).Returns(() => f.Context.Players.SingleOrDefaultAsync(p => p.UserId == 20));
            return f;
        }
        public IPlayerRepository PlayerRepository => players.Object;
        public ApplicationDbContext OpenContext() => new SqliteContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        public AssignDevelopmentSchoolCommand Command() => new() { AdminUserId = 40, AccountKey = Definition.AccountKey, CommunityId = 1, GradeId = 1, ClassId = 1 };
        public AssignDevelopmentSchoolCommandHandler Handler(IDevelopmentAuthenticationGuard? guard = null, IUserService? userService = null) => new(
            guard ?? Mock.Of<IDevelopmentAuthenticationGuard>(), userService ?? Users.Object, players.Object,
            new BaseRepository<Community>(Context), new BaseRepository<CommunityLicense>(Context),
            new BaseRepository<StudentLicense>(Context), new BaseRepository<CommunityUser>(Context),
            new BaseRepository<Grade>(Context), new BaseRepository<Domain.Models.Class>(Context), new StudentEnrollmentTransaction(Context));
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class SqliteContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
