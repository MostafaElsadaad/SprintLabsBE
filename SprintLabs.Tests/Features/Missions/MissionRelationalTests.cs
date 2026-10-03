using Domain.Models;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Exceptions;
using System.Net;
namespace Compass.Tests.Features.Missions;
public class MissionRelationalTests
{
    [Fact]
    public async Task Claim_failure_after_sql_rolls_back_balances_and_logs_then_fresh_scope_retries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var failure = new FailClaims();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).AddInterceptors(failure).Options;
        using var f = new MissionFixture(new TestContext(options)); await f.Context.Database.EnsureCreatedAsync(); await f.SeedAsync();
        var m = await f.AssignAsync("Boolean",1); await f.Service.EventsAsync(new() { f.Event() }, default);
        failure.Fail = true;
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.ClaimAsync(1, m.PlayerMissionId, default))).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using var verify = new MissionFixture(new TestContext(options));
        verify.Context.Players.Single(x => x.Id == 101).Experience.Should().Be(0);
        verify.Context.MissionClaimLogs.Should().BeEmpty(); verify.Context.PlayerXpLogs.Should().BeEmpty();
        verify.Context.PlayerMissions.Single().Status.Should().Be(Domain.Enums.PlayerMissionStatus.Completed);
        failure.Fail = false; var result = await verify.Service.ClaimAsync(1, m.PlayerMissionId, default);
        using var replay = new MissionFixture(new TestContext(options));
        (await replay.Service.ClaimAsync(1, m.PlayerMissionId, default)).Should().BeEquivalentTo(result);
        replay.Context.MissionClaimLogs.Count().Should().Be(2);
    }
    [Fact]
    public async Task Invalid_bulk_rolls_back_prior_event_and_progress_from_new_context()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var f = new MissionFixture(new TestContext(options)); await f.Context.Database.EnsureCreatedAsync(); await f.SeedAsync(); await f.AssignAsync("MaxValue");
        await Assert.ThrowsAsync<GenericException>(() => f.Service.EventsAsync(new() { f.Event(value: 1), f.Event(value: "invalid") }, default));
        using var verify = new MissionFixture(new TestContext(options));
        verify.Context.MissionEventLogs.Should().BeEmpty(); verify.Context.PlayerMissions.Single().CurrentProgress.Should().Be(0);
        (await verify.Service.GetMyAsync(1, default)).Should().HaveCount(1);
    }
    private sealed class TestContext : ApplicationDbContext
    {
        public TestContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder b) {
            base.OnModelCreating(b);
            foreach (var p in b.Model.GetEntityTypes().SelectMany(x => x.GetProperties())) if (p.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") p.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
    private sealed class FailClaims : SaveChangesInterceptor
    {
        public bool Fail { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
        {
            if (Fail && e.Context!.ChangeTracker.Entries<MissionClaimLog>().Any()) throw new DbUpdateException("Injected failure after SQL writes.");
            return base.SavedChangesAsync(e,result,ct);
        }
    }
}
