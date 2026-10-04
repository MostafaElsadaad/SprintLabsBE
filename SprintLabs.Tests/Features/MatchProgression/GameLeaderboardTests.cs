using System.Net;
using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Requests;

namespace Compass.Tests.Features.MatchProgression;

public class GameLeaderboardTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Own_standing_is_independent_of_page_and_ties_match_page_positions(bool relational)
    {
        using var f = await FixtureAsync(relational);
        var page = await f.Reads.GetLeaderboardAsync(1, null, new() { PageSize = 1 }, default);
        page.Items.Single().PlayerProfileId.Should().Be(102);
        page.Items.Single().Points.Should().Be(1000);
        page.CurrentPlayer!.Position.Should().Be(3);
        var me = await f.Reads.GetLeaderboardStandingAsync(3, null, new(), default);
        me.CurrentPlayer!.Position.Should().Be(2);
        me.Total.Should().Be(3);
        var empty = await f.Reads.GetLeaderboardAsync(1, null, new() { Page = 99 }, default);
        empty.Items.Should().BeEmpty(); empty.CurrentPlayer!.Position.Should().Be(3);
        page.PeriodStartsAt.Should().BeNull(); page.PeriodEndsAt.Should().BeNull();
        page.AvailableFilters.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Period_points_use_settled_ranked_rewards_and_stable_ties_not_current_rp(bool relational)
    {
        using var f = await FixtureAsync(relational);
        Reward(f, 101, 500, Now.AddMonths(-1));
        Reward(f, 101, 7, new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        Reward(f, 101, 30, Now.AddDays(-1));
        Reward(f, 101, -10, Now);
        Reward(f, 102, 20, Now);
        Reward(f, 103, 999, Now, type: Domain.Enums.MatchType.Friendly);
        Reward(f, 103, 999, Now, status: MatchStatus.Started);
        Reward(f, 103, 999, Now.AddMinutes(1));
        await f.Context.SaveChangesAsync();
        var week = await f.Reads.GetLeaderboardAsync(1, null, new() { Period = "week", PageSize = 1 }, default);
        week.Period.Should().Be("Week"); week.Total.Should().Be(2);
        week.Items.Single().PlayerProfileId.Should().Be(101);
        week.Items.Single().Points.Should().Be(20); week.Items.Single().Rp.Should().Be(450);
        (await f.Reads.GetLeaderboardStandingAsync(2, null, new() { Period = "Week" }, default)).CurrentPlayer!.Position.Should().Be(2);
        (await f.Reads.GetLeaderboardStandingAsync(3, null, new() { Period = "Week" }, default)).CurrentPlayer.Should().BeNull();
        var month = await f.Reads.GetLeaderboardAsync(1, null, new() { Period = "Month" }, default);
        month.CurrentPlayer!.Points.Should().Be(27);
        var year = await f.Reads.GetLeaderboardAsync(1, null, new() { Period = "Year" }, default);
        year.CurrentPlayer!.Points.Should().Be(527);
    }

    [Theory]
    [InlineData("Week", 2026, 10, 12, 2026, 10, 19)]
    [InlineData("Month", 2026, 10, 1, 2026, 11, 1)]
    [InlineData("Year", 2026, 1, 1, 2027, 1, 1)]
    public async Task Periods_use_utc_calendar_boundaries_and_include_start(string period,
        int sy, int sm, int sd, int ey, int em, int ed)
    {
        using var f = await FixtureAsync(true);
        var start = new DateTime(sy, sm, sd, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(ey, em, ed, 0, 0, 0, DateTimeKind.Utc);
        Reward(f, 101, 30, start); Reward(f, 101, 100, start.AddTicks(-1)); Reward(f, 101, 100, end);
        await f.Context.SaveChangesAsync();
        var board = await f.Reads.GetLeaderboardAsync(1, null, new() { Period = period }, default);
        board.PeriodStartsAt.Should().Be(start); board.PeriodEndsAt.Should().Be(end);
        board.PeriodStartsAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        board.CurrentPlayer!.Points.Should().Be(30);
    }

    [Fact]
    public async Task Zero_and_negative_period_points_are_ranked_and_int64_aggregation_does_not_overflow()
    {
        using var f = await FixtureAsync(true);
        Reward(f, 101, int.MaxValue, Now); Reward(f, 101, int.MaxValue, Now);
        Reward(f, 102, 0, Now); Reward(f, 103, -10, Now);
        await f.Context.SaveChangesAsync();
        var board = await f.Reads.GetLeaderboardAsync(1, null, new() { Period = "Week" }, default);
        board.Items.Select(x => x.Points).Should().Equal(2L * int.MaxValue, 0, -10);
        board.Items.Select(x => x.Position).Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Players_access_own_school_and_class_only_without_widening_private_progression(bool relational)
    {
        using var f = await FixtureAsync(relational);
        (await f.Reads.GetLeaderboardAsync(1, 1, new(), default)).Total.Should().Be(2);
        var ownClass = await f.Reads.GetLeaderboardAsync(1, 1, new() { ClassId = 1, GradeId = 1 }, default);
        ownClass.Items.Single().PlayerProfileId.Should().Be(101);
        ownClass.AvailableFilters.Single().ClassId.Should().Be(1);
        ownClass.AvailableFilters.Single().GradeId.Should().Be(1);
        (await f.Reads.GetLeaderboardAsync(20, 1, new(), default)).AvailableFilters.Single().ClassId.Should().Be(1);
        (await f.Reads.GetLeaderboardAsync(10, 1, new(), default)).AvailableFilters.Select(x => x.ClassId).Should().Equal(1, 2);
        (await f.Reads.GetLeaderboardStandingAsync(1, 1, new(), default)).CurrentPlayer!.Position.Should().Be(2);
        await ErrorAsync(() => f.Reads.GetLeaderboardAsync(1, 1, new() { ClassId = 2 }, default), HttpStatusCode.Forbidden);
        await ErrorAsync(() => f.Reads.GetLeaderboardAsync(1, 2, new(), default), HttpStatusCode.Forbidden);
        await ErrorAsync(() => f.Reads.GetLeaderboardAsync(1, 1, new() { ClassId = 3 }, default), HttpStatusCode.NotFound);
        await ErrorAsync(() => f.Reads.GetProgressionAsync(1, 102, default), HttpStatusCode.NotFound);
        await ErrorAsync(() => f.Reads.GetRankingAsync(1, 102, default), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task School_periods_count_only_matches_in_that_school()
    {
        using var f = await FixtureAsync(true);
        Reward(f, 101, 100, Now); Reward(f, 101, 3, Now, communityId: 1);
        Reward(f, 102, 20, Now, communityId: 1); Reward(f, 102, 900, Now, communityId: 2);
        await f.Context.SaveChangesAsync();
        var board = await f.Reads.GetLeaderboardAsync(1, 1, new() { Period = "Week" }, default);
        board.Items.Select(x => x.Points).Should().Equal(20, 3);
        board.CurrentPlayer!.Points.Should().Be(3);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("class")]
    [InlineData("community")]
    [InlineData("wrong-user")]
    [InlineData("wrong-profile")]
    public async Task Ineligible_or_mismatched_license_never_authorizes_school_access(string cause)
    {
        using var f = await FixtureAsync(true);
        var license = f.Context.StudentLicenses.Single(x => x.PlayerProfileId == 101);
        switch (cause)
        {
            case "revoked": license.Status = StudentLicenseStatus.Revoked; break;
            case "class": f.Context.Classes.Single(x => x.Id == 1).Status = ClassStatus.Deleted; break;
            case "community": f.Context.Communities.Single(x => x.Id == 1).Status = CommunityStatus.Suspended; break;
            case "wrong-user": license.UserId = 2; break;
            case "wrong-profile": license.PlayerProfileId = 102; break;
        }
        await f.Context.SaveChangesAsync();
        await ErrorAsync(() => f.Reads.GetLeaderboardAsync(1, 1, new(), default),
            cause == "community" ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Suspension_unlinked_profiles_and_missing_own_profile_are_handled()
    {
        using var f = await FixtureAsync(true);
        f.Context.Users.Single(x => x.Id == 2).Status = UserStatus.Suspended;
        f.Context.Players.Single(x => x.Id == 103).UserId = null;
        await f.Context.SaveChangesAsync();
        (await f.Reads.GetLeaderboardAsync(1, null, new(), default)).Total.Should().Be(1);
        await ErrorAsync(() => f.Reads.GetLeaderboardStandingAsync(2, null, new(), default), HttpStatusCode.Forbidden);
        await ErrorAsync(() => f.Reads.GetLeaderboardStandingAsync(20, null, new(), default), HttpStatusCode.NotFound);
        (await f.Reads.GetLeaderboardAsync(20, null, new(), default)).CurrentPlayer.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Daily")]
    [InlineData("1")]
    [InlineData("Weekly")]
    public async Task Unknown_periods_are_rejected(string period)
    {
        using var f = await FixtureAsync(false);
        await ErrorAsync(() => f.Reads.GetLeaderboardAsync(1, null, new() { Period = period }, default), HttpStatusCode.BadRequest);
    }

    private static async Task ErrorAsync(Func<Task> action, HttpStatusCode status)
        => (await Assert.ThrowsAsync<GenericException>(action)).StatusCode.Should().Be(status);

    private static async Task<ProgressionFixture> FixtureAsync(bool relational)
    {
        ApplicationDbContext? context = null;
        if (relational)
        {
            context = new SqliteLeaderboardContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=:memory:").Options);
            await context.Database.OpenConnectionAsync(); await context.Database.EnsureCreatedAsync();
        }
        var fixture = new ProgressionFixture(context, new FixedClock());
        await fixture.SeedAsync();
        return fixture;
    }

    private static void Reward(ProgressionFixture f, long playerId, int points, DateTime completedAt,
        long? communityId = null, Domain.Enums.MatchType type = Domain.Enums.MatchType.Ranked, MatchStatus status = MatchStatus.Completed)
    {
        var match = new Match { MatchCode = Guid.NewGuid().ToString(), MatchType = type, Status = status,
            CommunityId = communityId, StartedAt = completedAt.AddMinutes(-5), EndedAt = completedAt,
            CompletedAt = completedAt, TotalPlayers = 2 };
        f.Context.MatchRewardResults.Add(new MatchRewardResult { Match = match, PlayerProfileId = playerId,
            CommunityId = communityId, RpChange = points });
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private sealed class SqliteLeaderboardContext : ApplicationDbContext
    {
        public SqliteLeaderboardContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
