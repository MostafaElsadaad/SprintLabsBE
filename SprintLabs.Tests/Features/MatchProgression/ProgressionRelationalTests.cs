using System.Net;
using Domain.Models;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Exceptions;

namespace Compass.Tests.Features.MatchProgression;

public class ProgressionRelationalTests
{
    [Fact]
    public async Task Relational_completion_survives_a_new_context_and_preserves_answer_order()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var f = new ProgressionFixture(new SqliteProgressionContext(options));
        await f.Context.Database.EnsureCreatedAsync(); await f.SeedAsync();
        var registration = f.Registration(communityId: 1);
        var match = await f.Matches.RegisterAsync(registration, default);
        var original = await f.Matches.CompleteAsync(match.MatchId, f.Completion(communityId: 1), default);
        using var other = new ProgressionFixture(new SqliteProgressionContext(options));
        var retry = await other.Matches.CompleteAsync(match.MatchId, f.Completion(), default);
        retry.Should().BeEquivalentTo(original);
        (await other.Matches.RegisterAsync(registration, default)).MatchId.Should().Be(match.MatchId);
        var detail = await other.Reads.GetMatchAsync(1, match.MatchId, default);
        detail.QuestionResults.Select(x => x.IsCorrect).Should().Equal(true, true, false, true);
        (await other.Reads.GetLeaderboardAsync(20, 1, new(), default)).Total.Should().Be(1);
        (await other.Reads.GetHistoryAsync(20, 1, 101, new(), default)).Total.Should().Be(1);
        other.Context.Players.Single(x => x.Id == 101).TotalMatches.Should().Be(1);
    }

    [Fact]
    public async Task Database_failure_rolls_back_profiles_history_logs_and_match_state()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var interceptor = new FailRewardSaveInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).AddInterceptors(interceptor).Options;
        using var f = new ProgressionFixture(new SqliteProgressionContext(options));
        await f.Context.Database.EnsureCreatedAsync(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(), default);
        interceptor.Fail = true;
        var failure = await Assert.ThrowsAsync<GenericException>(() => f.Matches.CompleteAsync(match.MatchId, f.Completion(), default));
        failure.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using var verify = new SqliteProgressionContext(options);
        verify.MatchRewardResults.Should().BeEmpty(); verify.MatchQuestionResults.Should().BeEmpty();
        verify.PlayerXpLogs.Should().BeEmpty(); verify.PlayerRankLogs.Should().BeEmpty();
        verify.Players.Should().OnlyContain(x => x.Experience == 0 && x.TotalMatches == 0);
        verify.Matches.Single().Status.Should().Be(Domain.Enums.MatchStatus.Started);
        // Retry from a fresh request scope must now complete normally.
        interceptor.Fail = false;
        using var retry = new ProgressionFixture(new SqliteProgressionContext(options));
        (await retry.Matches.CompleteAsync(match.MatchId, f.Completion(), default)).Rewards.Should().HaveCount(2);
    }

    private sealed class SqliteProgressionContext : ApplicationDbContext
    {
        public SqliteProgressionContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Test-only SQL dialect adaptation; production uses the unchanged MySQL mappings.
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    private sealed class FailRewardSaveInterceptor : SaveChangesInterceptor
    {
        public bool Fail { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            // Fail after SQL has written all rows, before the completion service commits its transaction.
            if (Fail && eventData.Context!.ChangeTracker.Entries<MatchRewardResult>().Any())
                throw new DbUpdateException("Injected persistence failure.");
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
