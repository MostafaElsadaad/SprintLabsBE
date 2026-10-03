using Compass.Tests.Features.CurrentCommunityResolution;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Shared.Exceptions;
using Shared.Requests;
using System.Net;
namespace Compass.Tests.Features.Missions;
public class MissionMysqlConcurrencyTests
{
    [MysqlFact]
    public async Task Concurrent_event_retries_progress_once()
    {
        await using var db = await MissionMysqlDatabase.CreateAsync();
        using var f = new MissionFixture(db.Context()); await f.SeedAsync(); await f.AssignAsync(); var e = f.Event();
        async Task Run() => await Retry(db, s => s.Service.EventsAsync(new() { e }, default));
        await Task.WhenAll(Run(), Run());
        using var verify = db.Context(); verify.MissionEventLogs.Count().Should().Be(1); verify.PlayerMissions.Single().CurrentProgress.Should().Be(1);
    }
    [MysqlFact]
    public async Task Concurrent_claims_reward_once()
    {
        await using var db = await MissionMysqlDatabase.CreateAsync();
        using var f = new MissionFixture(db.Context()); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() }, default);
        var results = await Task.WhenAll(Retry(db, s => s.Service.ClaimAsync(1,m.PlayerMissionId,default)), Retry(db, s => s.Service.ClaimAsync(1,m.PlayerMissionId,default)));
        results[0].Should().BeEquivalentTo(results[1]);
        using var verify = db.Context(); verify.Players.Single(x => x.Id == 101).Experience.Should().Be(50); verify.MissionClaimLogs.Count().Should().Be(2);
    }
    [MysqlFact]
    public async Task Concurrent_assignment_preserves_one_random_selection()
    {
        await using var db = await MissionMysqlDatabase.CreateAsync();
        using var f = new MissionFixture(db.Context()); await f.SeedAsync(); await f.Service.SeedAsync(40,default);
        var results = await Task.WhenAll(Retry(db,s => s.Service.GetMyAsync(1,default)), Retry(db,s => s.Service.GetMyAsync(1,default)));
        results[0].Should().BeEquivalentTo(results[1]);
        using var verify = db.Context(); verify.PlayerMissions.Count().Should().Be(4); verify.PlayerMissionAssignments.Count().Should().Be(3);
    }
    [MysqlFact]
    public async Task Match_rewards_and_mission_claim_do_not_lose_xp_updates()
    {
        await using var db = await MissionMysqlDatabase.CreateAsync();
        using var f = new MissionFixture(db.Context()); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() },default);
        var match = await f.Base.Matches.RegisterAsync(f.Base.Registration(),default); var completion = f.Base.Completion();
        await Task.WhenAll(Retry(db,s => s.Service.ClaimAsync(1,m.PlayerMissionId,default)), Retry(db,s => s.Base.Matches.CompleteAsync(match.MatchId,completion,default)));
        using var verify = db.Context(); verify.Players.Single(x => x.Id == 101).Experience.Should().Be(132);
    }
    private static async Task<T> Retry<T>(MissionMysqlDatabase db, Func<MissionFixture,Task<T>> action)
    {
        for (var attempt = 0; ; attempt++) {
            using var f = new MissionFixture(db.Context());
            try { return await action(f); }
            catch (GenericException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 3) { await Task.Delay(50); }
        }
    }
    private sealed class MissionMysqlDatabase : IAsyncDisposable
    {
        private readonly string _connection;
        private MissionMysqlDatabase(string connection) => _connection = connection;
        public ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(_connection, new MySqlServerVersion(new Version(8, 0, 36))).Options);
        public static async Task<MissionMysqlDatabase> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("SPRINTLABS_MYSQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("A dedicated MySQL test connection is required.");
            var builder = new MySqlConnectionStringBuilder(connection);
            if (!builder.Database.StartsWith("sprintlabs-test", StringComparison.OrdinalIgnoreCase) &&
                !builder.Database.StartsWith("compass-test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A dedicated test database prefix is required.");
            // Each test creates and removes only this unique database, never the supplied base database.
            builder.Database = "sprintlabs-test-missions-" + Guid.NewGuid().ToString("N");
            var database = new MissionMysqlDatabase(builder.ConnectionString);
            using var context = database.Context(); await context.Database.EnsureCreatedAsync();
            return database;
        }
        public async ValueTask DisposeAsync()
        {
            using var context = Context(); await context.Database.EnsureDeletedAsync();
        }
    }
}
