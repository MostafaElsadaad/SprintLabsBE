using Compass.Tests.Features.CurrentCommunityResolution;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Responses;

namespace Compass.Tests.Features.MatchProgression;

public class ProgressionMysqlConcurrencyTests
{
    [MysqlFact]
    public async Task Leaderboard_period_aggregates_and_own_positions_execute_on_mysql()
    {
        await using var database = await MysqlProgressionDatabase.CreateAsync();
        using var f = new ProgressionFixture(database.Context()); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(communityId: 1), default);
        await f.Matches.CompleteAsync(match.MatchId, f.Completion(communityId: 1), default);
        foreach (var period in new[] { "AllTime", "Week", "Month", "Year" })
        {
            var board = await f.Reads.GetLeaderboardAsync(1, 1, new() { Period = period, PageSize = 1 }, default);
            var me = await f.Reads.GetLeaderboardStandingAsync(1, 1, new() { Period = period }, default);
            me.CurrentPlayer.Should().BeEquivalentTo(board.CurrentPlayer);
            board.Total.Should().Be(2);
            if (period != "AllTime")
            {
                board.Items.Single().PlayerProfileId.Should().Be(101);
                me.CurrentPlayer!.Points.Should().Be(30);
                me.CurrentPlayer.Position.Should().Be(1);
            }
        }
    }

    [MysqlFact]
    public async Task Same_match_parallel_completions_reward_once()
    {
        await using var database = await MysqlProgressionDatabase.CreateAsync();
        using var seed = new ProgressionFixture(database.Context()); await seed.SeedAsync();
        var match = await seed.Matches.RegisterAsync(seed.Registration(), default);
        using var first = new ProgressionFixture(database.Context());
        using var second = new ProgressionFixture(database.Context());
        var results = await Task.WhenAll(first.Matches.CompleteAsync(match.MatchId, seed.Completion(), default),
            second.Matches.CompleteAsync(match.MatchId, seed.Completion(), default));
        results[0].Should().BeEquivalentTo(results[1]);
        using var verify = database.Context();
        verify.Players.Should().OnlyContain(x => x.Id == 103 || x.TotalMatches == 1);
        verify.MatchRewardResults.Count().Should().Be(2); verify.PlayerXpLogs.Count().Should().Be(5);
    }

    [MysqlFact]
    public async Task Different_matches_parallel_completions_do_not_lose_profile_updates()
    {
        await using var database = await MysqlProgressionDatabase.CreateAsync();
        using var seed = new ProgressionFixture(database.Context()); await seed.SeedAsync();
        var a = await seed.Matches.RegisterAsync(seed.Registration(), default);
        var b = await seed.Matches.RegisterAsync(seed.Registration(), default);
        await Task.WhenAll(CompleteWithRetry(database, a.MatchId, seed.Completion()), CompleteWithRetry(database, b.MatchId, seed.Completion()));
        using var verify = database.Context();
        var winner = verify.Players.Single(x => x.Id == 101);
        winner.Experience.Should().Be(164); winner.TotalMatches.Should().Be(2); winner.Rp.Should().Be(510);
        verify.MatchRewardResults.Count().Should().Be(4);
    }

    [MysqlFact]
    public async Task Concurrent_registration_converges_on_one_backend_match_id()
    {
        await using var database = await MysqlProgressionDatabase.CreateAsync();
        using var seed = new ProgressionFixture(database.Context()); await seed.SeedAsync();
        var request = seed.Registration();
        async Task<MatchRegistrationResponse> Register()
        {
            for (var attempt = 0; ; attempt++)
            {
                using var scope = new ProgressionFixture(database.Context());
                try { return await scope.Matches.RegisterAsync(request, default); }
                catch (GenericException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < 2)
                { await Task.Delay(50); }
            }
        }
        var results = await Task.WhenAll(Register(), Register());
        results[0].MatchId.Should().Be(results[1].MatchId);
        using var verify = database.Context(); verify.Matches.Count().Should().Be(1);
    }

    private static async Task CompleteWithRetry(MysqlProgressionDatabase database, long id, CompleteMatchRequest request)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var scope = new ProgressionFixture(database.Context());
            try { await scope.Matches.CompleteAsync(id, request, default); return; }
            catch (GenericException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < 2)
            { await Task.Delay(50); }
        }
    }

    private sealed class MysqlProgressionDatabase : IAsyncDisposable
    {
        private readonly string _connection;
        private MysqlProgressionDatabase(string connection) => _connection = connection;
        public ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(_connection, new MySqlServerVersion(new Version(8, 0, 36))).Options);
        public static async Task<MysqlProgressionDatabase> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("SPRINTLABS_MYSQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("A dedicated MySQL test connection is required.");
            var builder = new MySqlConnectionStringBuilder(connection);
            if (!builder.Database.StartsWith("sprintlabs-test", StringComparison.OrdinalIgnoreCase) &&
                !builder.Database.StartsWith("compass-test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A dedicated test database prefix is required.");
            // Each test creates and removes only this unique database, never the supplied base database.
            builder.Database = "sprintlabs-test-progression-" + Guid.NewGuid().ToString("N");
            var database = new MysqlProgressionDatabase(builder.ConnectionString);
            using var context = database.Context(); await context.Database.EnsureCreatedAsync();
            return database;
        }
        public async ValueTask DisposeAsync()
        {
            using var context = Context(); await context.Database.EnsureDeletedAsync();
        }
    }
}
