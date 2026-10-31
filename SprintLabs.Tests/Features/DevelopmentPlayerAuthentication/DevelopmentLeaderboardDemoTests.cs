using System.Net;
using Application.Features.Accounts.DevelopmentAuthentication.SeedLeaderboardDemo;
using Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using FluentAssertions;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DevelopmentAuthentication;
using Shared.Exceptions;
using Shared.Responses;
using Shared.Requests;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentLeaderboardDemoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Seed_creates_three_classes_period_history_and_is_repeat_safe(bool relational)
    {
        using var f = new Fixture(relational);
        var before = f.Context.Players.Single(p => p.Id == 101).Experience;
        var result = await f.Handler().Handle(f.Command(), default);
        result.Players.Should().HaveCount(8);
        result.Players.Select(p => p.ClassId).Distinct().Should().HaveCount(3);
        f.Context.Matches.Should().HaveCount(3);
        f.Context.MatchRewardResults.Should().HaveCount(24);
        var now = DateTime.UtcNow;
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var week = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var year = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        f.Context.Matches.Select(m => m.CompletedAt).Should().BeEquivalentTo(new DateTime?[] { year, month, week });
        var reads = new ProgressionReadService(f.Context, new Infrastructure.Services.LevelProgressionService(),
            new CommunityAccessService(new BaseRepository<CommunityUser>(f.Context)));
        var weekly = await reads.GetLeaderboardAsync(40, 1, new() { Period = "Week" }, default);
        var monthly = await reads.GetLeaderboardAsync(40, 1, new() { Period = "Month" }, default);
        var yearly = await reads.GetLeaderboardAsync(40, 1, new() { Period = "Year" }, default);
        weekly.Items.Should().HaveCount(8);
        if (week > month) weekly.Items.Select(p => p.Points).Should().NotEqual(monthly.Items.Select(p => p.Points));
        if (month > year) monthly.Items.Select(p => p.Points).Should().NotEqual(yearly.Items.Select(p => p.Points));
        var scoped = await reads.GetLeaderboardAsync(40, 1, new() { ClassId = result.Players[0].ClassId }, default);
        scoped.Total.Should().Be(3);
        var xp = f.Context.Players.ToDictionary(p => p.Id, p => p.Experience);
        xp[101].Should().BeGreaterThan(before);
        await f.Handler().Handle(f.Command(), default);
        f.Context.Matches.Should().HaveCount(3);
        f.Context.MatchRewardResults.Should().HaveCount(24);
        f.Context.Players.ToDictionary(p => p.Id, p => p.Experience).Should().BeEquivalentTo(xp);
        f.Context.CommunityLicenses.Single().UsedStudents.Should().Be(8);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("locked")]
    [InlineData("provider")]
    [InlineData("school")]
    [InlineData("base-collision")]
    public async Task Invalid_operator_or_non_demo_identity_cannot_seed(string reason)
    {
        using var f = new Fixture();
        if (reason == "admin") f.Admin.IsPlatformAdmin = false;
        if (reason == "locked") f.Admin.IsLockedOut = true;
        if (reason == "provider") f.Identities[0].FirebaseUid = "real-provider";
        if (reason == "base-collision") f.Context.Classes.Single().Name = "Demo Class B";
        if (reason == "school") f.Context.Communities.Single().Status = CommunityStatus.Suspended;
        await f.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<GenericException>(() => f.Handler().Handle(f.Command(), default));
        f.Context.Matches.Should().BeEmpty();
        f.Context.MatchRewardResults.Should().BeEmpty();
    }

    [Fact]
    public async Task Month_rollover_within_one_week_reuses_year_week_and_adds_only_new_month()
    {
        using var f = new Fixture();
        f.Clock.Now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        await f.Handler().Handle(f.Command(), default);
        f.Clock.Now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        await f.Handler().Handle(f.Command(), default);
        f.Context.Matches.Should().HaveCount(4);
        f.Context.MatchRewardResults.Should().HaveCount(32);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_reward_committed_between_demo_matches_is_not_overwritten(bool relational)
    {
        using var baseline = new Fixture(relational);
        await baseline.Handler().Handle(baseline.Command(), default);
        var expected = baseline.Context.Players.Single(p => p.Id == 101);
        using var f = new Fixture(relational);
        f.AfterFirstCompletion = () => f.AddConcurrentReward();
        await f.Handler().Handle(f.Command(), default);
        var actual = f.Context.Players.Single(p => p.Id == 101);
        actual.Experience.Should().Be(expected.Experience + 900);
        actual.Gold.Should().Be(expected.Gold + 700);
        actual.TotalMatches.Should().Be(expected.TotalMatches + 1);
    }
    private sealed class SqliteContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class InterleavingProgression(IMatchProgressionService inner, Action afterComplete) : IMatchProgressionService
    {
        public Task<MatchRegistrationResponse> RegisterAsync(RegisterMatchRequest r, CancellationToken ct) => inner.RegisterAsync(r, ct);
        public async Task<MatchCompletionResponse> CompleteAsync(long id, CompleteMatchRequest r, CancellationToken ct)
        {
            var result = await inner.CompleteAsync(id, r, ct);
            afterComplete();
            return result;
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> options;
        private readonly bool relational;
        private readonly string? databasePath;
        private ApplicationDbContext OpenContext() => relational ? new SqliteContext(options) : new ApplicationDbContext(options);
        public ApplicationDbContext Context { get; }
        private readonly ServiceProvider services;
        public MutableClock Clock { get; } = new();
        public Action? AfterFirstCompletion { get; set; }
        private int completions;
        public void AddConcurrentReward()
        {
            using var concurrent = OpenContext();
            var player = concurrent.Players.Single(p => p.Id == 101);
            player.Experience += 900; player.Gold += 700; player.TotalMatches++;
            concurrent.SaveChanges();
        }
        public UserIdentityResponse Admin { get; } = new() { Id = 40, IsPlatformAdmin = true, Status = "Active" };
        public List<UserIdentityResponse> Identities { get; } = new();
        private readonly Mock<IUserService> users = new();
        private readonly Mock<IDevelopmentAuthenticationGuard> guard = new();
        public Fixture(bool relational = false)
        {
            this.relational = relational;
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (relational)
            {
                databasePath = Path.Combine(Path.GetTempPath(), $"SprintLab-demo-{Guid.NewGuid():N}.db");
                builder.UseSqlite($"Data Source={databasePath};Pooling=False");
            }
            else builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options = builder.Options;
            Context = OpenContext();
            Context.Database.EnsureCreated();
            services = new ServiceCollection().AddScoped(_ => OpenContext())
                                .AddScoped<MatchProgressionService>()
                .AddScoped<IMatchProgressionService>(sp => new InterleavingProgression(sp.GetRequiredService<MatchProgressionService>(),
                    () => { if (completions++ == 0) AfterFirstCompletion?.Invoke(); }))
                .AddSingleton<IXpCalculationService, Infrastructure.Services.XpCalculationService>()
                .AddSingleton<ILevelProgressionService, Infrastructure.Services.LevelProgressionService>()
                .AddSingleton<IRpRankCalculationService, Infrastructure.Services.RpRankCalculationService>().BuildServiceProvider();
            users.Setup(u => u.GetCurrentUser(40)).ReturnsAsync(Admin);
            Context.Users.Add(new User { Id = 40, UserName = "admin", Name = "Admin", Email = "a@example.com", IsPlatformAdmin = true });
            for (int i = 0; i < 8; i++)
            {
                var d = DevelopmentPlayerCatalog.All[i];
                var identity = new UserIdentityResponse { Id = i + 1, UserName = d.AccountKey,
                    Email = d.Email, Status = "Active", PlayerProfileId = 101 + i };
                Identities.Add(identity);
                users.Setup(u => u.FindByEmail(d.Email)).ReturnsAsync(identity);
                Context.Users.Add(new User { Id = i + 1, UserName = d.AccountKey, Name = d.DisplayName, Email = d.Email });
                Context.Players.Add(new Player { Id = 101 + i, UserId = i + 1, Email = d.Email, Name = d.DisplayName, Experience = i * 10 });
            }
            Context.Communities.Add(new Community { Id = 1, Name = "Demo School", Slug = "demo" });
            Context.CommunityLicenses.Add(new CommunityLicense { CommunityId = 1, MaxStudents = 20 });
            Context.Grades.Add(new Grade { Id = 1, CommunityId = 1, Name = "Grade 7", Value = 7 });
            Context.Classes.Add(new Class { Id = 1, CommunityId = 1, GradeId = 1, Name = "Class 7A" });
            Context.SaveChanges();
        }
        public SeedLeaderboardDemoCommand Command() => new() { AdminUserId = 40, CommunityId = 1, GradeId = 1, BaseClassId = 1 };
        public SeedLeaderboardDemoCommandHandler Handler()
        {
            var assignment = new AssignDevelopmentSchoolCommandHandler(guard.Object, users.Object, new PlayerRepository(Context),
                new BaseRepository<Community>(Context), new BaseRepository<CommunityLicense>(Context),
                new BaseRepository<StudentLicense>(Context), new BaseRepository<CommunityUser>(Context),
                new BaseRepository<Grade>(Context), new BaseRepository<Class>(Context), new StudentEnrollmentTransaction(Context));
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<AssignDevelopmentSchoolCommand>(), It.IsAny<CancellationToken>()))
                .Returns((AssignDevelopmentSchoolCommand c, CancellationToken ct) => assignment.Handle(c, ct));
            return new(guard.Object, users.Object, new PlayerRepository(Context), new BaseRepository<Community>(Context),
                new BaseRepository<Grade>(Context), new BaseRepository<Class>(Context), new BaseRepository<Domain.Models.Match>(Context),
                new StudentEnrollmentTransaction(Context), mediator.Object,
                services.GetRequiredService<IServiceScopeFactory>(), Clock);
        }
        public void Dispose()
        {
            services.Dispose(); Context.Dispose();
            if (databasePath != null) File.Delete(databasePath);
        }
    }
}