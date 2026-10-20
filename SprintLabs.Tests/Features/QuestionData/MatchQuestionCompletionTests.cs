using Domain.Models.QuestionData;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Requests.QuestionData;

namespace Compass.Tests.Features.QuestionData;

public class MatchQuestionCompletionTests
{
    [Fact]
    public async Task Completion_commits_typed_answers_and_rewards_once_and_preserves_seconds()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var service = new MatchProgressionService(f.Context, new Infrastructure.Services.XpCalculationService(), new Infrastructure.Services.LevelProgressionService(), new Infrastructure.Services.RpRankCalculationService());
        var q = QuestionDataTypesTests.Question("TrueOrFalse");
        await f.Service.PublishAsync(40, q, default);
        var registered = await service.RegisterAsync(new RegisterMatchRequest { MatchCode = Guid.NewGuid().ToString(),
            MatchType = "Ranked", TotalPlayers = 2, PlayerProfileIds = new() { 101, 102 }, StartedAt = DateTime.UtcNow.AddMinutes(-1) }, default);
        Assert.True(registered.SupportsQuestionHistory);
        var body = Completion(registered.MatchId, q.QuestionId);
        var result = await service.CompleteAsync(registered.MatchId, body, default);
        Assert.True(result.Rewards.Single(x => x.PlayerProfileId == 101).XpGained > 0);
        Assert.Equal(1.234m, (await f.Context.Set<QuestionHistory>().SingleAsync()).TimeTakenSeconds);
        Assert.False((await f.Context.Set<TrueFalseHistory>().SingleAsync()).SelectedAnswer);
        var beforeXp = (await f.Context.Players.SingleAsync(x => x.Id == 101)).Experience;
        await service.CompleteAsync(registered.MatchId, body, default);
        Assert.Equal(1, await f.Context.Set<QuestionHistory>().CountAsync());
        Assert.Equal(beforeXp, (await f.Context.Players.SingleAsync(x => x.Id == 101)).Experience);
    }

    [Theory]
    [InlineData("player")]
    [InlineData("time")]
    [InlineData("correctness")]
    [InlineData("write")]
    public async Task Invalid_typed_response_leaves_history_and_rewards_unchanged(string invalid)
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var service = new MatchProgressionService(f.Context, new Infrastructure.Services.XpCalculationService(), new Infrastructure.Services.LevelProgressionService(), new Infrastructure.Services.RpRankCalculationService());
        var q = QuestionDataTypesTests.Question("TrueOrFalse"); await f.Service.PublishAsync(40, q, default);
        var registered = await service.RegisterAsync(new RegisterMatchRequest { MatchCode = Guid.NewGuid().ToString(), MatchType = "Ranked",
            TotalPlayers = 2, PlayerProfileIds = new() { 101, 102 }, StartedAt = DateTime.UtcNow.AddMinutes(-1) }, default);
        var body = Completion(registered.MatchId, q.QuestionId);
        if (invalid == "player") body.QuestionResults[0].QuestionResponse!.PlayerId = 102;
        if (invalid == "time") body.QuestionResults[0].QuestionResponse!.TimeTakenSeconds = 9;
        if (invalid == "correctness") body.QuestionResults[0].QuestionResponse!.SelectedAnswer = true;
        if (invalid == "write") await f.Context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectXp BEFORE INSERT ON PlayerXpLogs BEGIN SELECT RAISE(ABORT, 'test write failure'); END");
        await Assert.ThrowsAsync<GenericException>(() => service.CompleteAsync(registered.MatchId, body, default));
        f.Context.ChangeTracker.Clear();
        Assert.Empty(await f.Context.Set<QuestionHistory>().ToListAsync());
        Assert.Empty(await f.Context.MatchRewardResults.ToListAsync());
        Assert.Equal(0, (await f.Context.Players.SingleAsync(x => x.Id == 101)).Experience);
    }

    [Fact]
    public async Task Ordering_projection_preserves_correct_shared_ids_when_labels_are_identical()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("Ordering");
        q.Items = new() { new() { ItemId = "first", ItemText = "same", CorrectPosition = 1 },
            new() { ItemId = "second", ItemText = "same", CorrectPosition = 0 } };
        await f.Service.PublishAsync(40, q, default);
        var response = await f.Service.ProjectBankAsync(new QuestionBankFilter { Grade = 7 }, default);
        Assert.Equal(new[] { "second", "first" }, response!.Value.GetProperty("Questions")[0]
            .GetProperty("Ordering").GetProperty("CorrectItemIds").EnumerateArray().Select(x => x.GetString()));
    }

    private static CompleteMatchRequest Completion(long match, Guid question) => new()
    {
        MatchType = "Ranked", EndedAt = DateTime.UtcNow,
        Players = new() { new() { PlayerProfileId = 101, Position = 1, CorrectAnswers = 1, MaxStreak = 1,
            AnswerTimeTotalMs = 1234, QuestionTimeTotalMs = 10000 }, new() { PlayerProfileId = 102, Position = 2 } },
        QuestionResults = new() { new() { PlayerProfileId = 101, QuestionType = "TrueOrFalse", IsCorrect = true,
            AnswerTimeMs = 1234, QuestionTimeMs = 10000, StreakAfterAnswer = 1,
            QuestionResponse = new QuestionHistoryRequest { PlayerId = 101, MatchId = match, QuestionId = question,
                TimeTakenSeconds = 1.234m, SelectedAnswer = false } } }
    };
}
