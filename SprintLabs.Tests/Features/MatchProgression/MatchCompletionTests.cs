using System.Net;
using System.Text.Json;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Requests;

namespace Compass.Tests.Features.MatchProgression;

public class MatchCompletionTests
{
    [Fact]
    public async Task Completion_saves_facts_rewards_and_logs_and_retry_returns_original_result()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        f.Context.Players.Single(x => x.Id == 101).Experience = 145;
        await f.Context.SaveChangesAsync();
        var registration = f.Registration();
        var match = await f.Matches.RegisterAsync(registration, default);
        var result = await f.Matches.CompleteAsync(match.MatchId, f.Completion(), default);
        var winner = result.Rewards.Single(x => x.PlayerProfileId == 101);
        winner.AnswerXp.Should().Be(32); winner.XpGained.Should().Be(82);
        winner.NewTotalXp.Should().Be(227); winner.NewLevel.Should().Be(2);
        winner.RpChange.Should().Be(30); winner.NewRp.Should().Be(480);
        result.Rewards.Single(x => x.PlayerProfileId == 102).RpChange.Should().Be(-23);
        f.Context.Players.Single(x => x.Id == 101).TotalWins.Should().Be(1);
        f.Context.MatchQuestionResults.Should().HaveCount(4);
        f.Context.PlayerXpLogs.Should().HaveCount(5);
        f.Context.PlayerXpLogs.Where(x => x.PlayerProfileId == 101).Sum(x => x.FinalXp).Should().Be(82);
        f.Context.PlayerRankLogs.Should().HaveCount(2);
        // Even a changed retry cannot rewrite facts or grant another reward.
        var retry = await f.Matches.CompleteAsync(match.MatchId, new CompleteMatchRequest(), default);
        JsonSerializer.Serialize(retry).Should().Be(JsonSerializer.Serialize(result));
        f.Context.MatchRewardResults.Should().HaveCount(2);
        f.Context.PlayerXpLogs.Should().HaveCount(5);
        f.Context.Players.Single(x => x.Id == 101).TotalMatches.Should().Be(1);
        (await f.Matches.RegisterAsync(registration, default)).MatchId.Should().Be(match.MatchId);
    }

    [Theory]
    [InlineData("Friendly")]
    [InlineData("Private")]
    public async Task Non_ranked_matches_keep_rp_and_rank(string type)
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        f.Context.Players.Single(x => x.Id == 101).RankTier = RankTier.Immortal;
        await f.Context.SaveChangesAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(type), default);
        var result = await f.Matches.CompleteAsync(match.MatchId, f.Completion(type), default);
        result.Rewards.Should().OnlyContain(x => x.RpChange == 0 && x.OldRp == x.NewRp);
        result.Rewards.Single(x => x.PlayerProfileId == 101).NewRankTier.Should().Be("Immortal");
        f.Context.PlayerRankLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("aggregate")]
    [InlineData("streak")]
    [InlineData("time")]
    [InlineData("outsider")]
    [InlineData("duplicate")]
    [InlineData("placement")]
    [InlineData("roster")]
    [InlineData("type")]
    [InlineData("community")]
    [InlineData("room")]
    [InlineData("end")]
    [InlineData("null-answer")]
    public async Task Invalid_facts_cannot_write_partial_rewards(string invalid)
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(), default);
        var request = f.Completion();
        switch (invalid)
        {
            case "aggregate": request.Players[0].CorrectAnswers++; break;
            case "streak": request.QuestionResults[3].StreakAfterAnswer = 9; break;
            case "time": request.QuestionResults[0].AnswerTimeMs = 20000; break;
            case "outsider": request.QuestionResults[0].PlayerProfileId = 103; break;
            case "duplicate": request.Players.Add(request.Players[0]); break;
            case "placement": request.Players[1].Position = 1; break;
            case "roster": request.Players[1].PlayerProfileId = 103; break;
            case "type": request.MatchType = "Friendly"; break;
            case "community": request.CommunityId = 1; break;
            case "room": request.MirrorRoomId = "different"; break;
            case "end": request.EndedAt = DateTime.UtcNow.AddHours(1); break;
            case "null-answer": request.QuestionResults.Add(null!); break;
        }
        var error = await Assert.ThrowsAsync<GenericException>(() => f.Matches.CompleteAsync(match.MatchId, request, default));
        error.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        f.Context.MatchRewardResults.Should().BeEmpty(); f.Context.PlayerXpLogs.Should().BeEmpty();
        f.Context.MatchQuestionResults.Should().BeEmpty();
        f.Context.Matches.Single().Status.Should().Be(MatchStatus.Started);
        f.Context.Players.Should().OnlyContain(x => x.Experience == 0 && x.TotalMatches == 0);
    }

    [Fact]
    public async Task Registered_bot_slots_affect_percentile_without_creating_bot_rewards()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(totalPlayers: 4), default);
        var request = f.Completion(); request.Players[0].Position = 2; request.Players[1].Position = 4;
        var result = await f.Matches.CompleteAsync(match.MatchId, request, default);
        result.Rewards.Should().HaveCount(2);
        result.Rewards.Single(x => x.PlayerProfileId == 101).RpChange.Should().Be(10);
        result.Rewards.Single(x => x.PlayerProfileId == 101).MatchResultXp.Should().Be(20);
        f.Context.Players.Sum(x => x.TotalWins).Should().Be(0);
    }

    [Fact]
    public async Task Match_state_missing_profiles_and_overflow_are_controlled_errors()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Matches.CompleteAsync(999, f.Completion(), default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var match = await f.Matches.RegisterAsync(f.Registration(), default);
        f.Context.Matches.Single().Status = MatchStatus.Cancelled;
        await f.Context.SaveChangesAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Matches.CompleteAsync(match.MatchId, f.Completion(), default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        f.Context.Matches.Single().Status = MatchStatus.Started;
        f.Context.Players.Single(x => x.Id == 101).Experience = int.MaxValue;
        await f.Context.SaveChangesAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Matches.CompleteAsync(match.MatchId, f.Completion(), default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        f.Context.MatchRewardResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("code")]
    [InlineData("type")]
    [InlineData("time")]
    [InlineData("bots")]
    [InlineData("ancient-time")]
    [InlineData("ranked-single")]
    [InlineData("community")]
    [InlineData("suspended")]
    public async Task Registration_rejects_invalid_or_ineligible_rosters(string invalid)
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var request = f.Registration();
        switch (invalid)
        {
            case "identity": request.PlayerProfileIds[1] = 999; break;
            case "code": request.MatchCode = " "; break;
            case "type": request.MatchType = "1"; break;
            case "time": request.StartedAt = DateTime.UtcNow.AddHours(1); break;
            case "ancient-time": request.StartedAt = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc); break;
            case "bots": request.TotalPlayers = 1; break;
            case "ranked-single": request.PlayerProfileIds.RemoveAt(1); request.TotalPlayers = 1; break;
            case "community": request.CommunityId = 2; break;
            case "suspended": f.Context.Users.Single(x => x.Id == 2).Status = UserStatus.Suspended; await f.Context.SaveChangesAsync(); break;
        }
        await Assert.ThrowsAsync<GenericException>(() => f.Matches.RegisterAsync(request, default));
        f.Context.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task Match_code_is_unique_and_changed_registration_is_conflict()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var request = f.Registration(); await f.Matches.RegisterAsync(request, default);
        request.MatchType = "Friendly";
        (await Assert.ThrowsAsync<GenericException>(() => f.Matches.RegisterAsync(request, default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        f.Context.Model.FindEntityType(typeof(Domain.Models.Match))!.GetIndexes()
            .Single(x => x.Properties.SingleOrDefault()?.Name == "MatchCode").IsUnique.Should().BeTrue();
    }
}
