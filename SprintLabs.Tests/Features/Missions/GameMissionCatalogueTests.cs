using Domain.Enums;
using FluentAssertions;
using Shared.Exceptions;
using System.Text.Json;

namespace Compass.Tests.Features.Missions;
public class GameMissionCatalogueTests
{
    [Fact]
    public async Task Game_catalogue_is_repeat_safe_and_assigns_same_weekly_and_seasonal_missions()
    {
        using var f = new MissionFixture(); await f.SeedAsync();
        var activations = await f.Service.SeedGameCatalogueAsync(40, default);
        (await f.Service.SeedGameCatalogueAsync(40, default)).Should().Equal(activations);
        f.Context.MissionTemplates.Count().Should().Be(56);
        f.Context.MissionActivations.Count().Should().Be(4);
        var a = await f.Service.GetMyAsync(1, default);
        var b = await f.Service.GetMyAsync(2, default);
        a.Should().HaveCount(56);
        a.Select(x => x.MissionKey).Should().BeEquivalentTo(b.Select(x => x.MissionKey));
        a.Select(x => x.PeriodType).Distinct().Should().BeEquivalentTo(new[] { "Weekly", "Seasonal" });
        f.Context.MissionActivationItems.All(x => x.AssignmentMode == MissionAssignmentMode.Fixed).Should().BeTrue();
    }

    [Fact]
    public async Task Dice_and_monster_events_complete_missions_once_and_claim_history_survives_period_end()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.Service.SeedGameCatalogueAsync(40, default);
        var match = await f.Base.Matches.RegisterAsync(f.Base.Registration(), default);
        var events = new List<Shared.Requests.MissionEventRequest>();
        foreach (var kind in new[] { "DiceRolled", "DiceRolled", "MonsterDefeated" })
        {
            var e = f.Event(kind); e.OccurredAt = DateTime.UtcNow; e.MatchId = match.MatchId;
            e.EventData = kind == "DiceRolled" ? new() { ["diceValue"] = JsonSerializer.SerializeToElement(6) }
                : new() { ["monsterKey"] = JsonSerializer.SerializeToElement("fog") };
            events.Add(e);
        }
        await f.Service.EventsAsync(events, default);
        (await f.Service.EventsAsync(events, default)).All(x => x.Duplicate).Should().BeTrue();
        var current = await f.Service.GetMyAsync(1, default);
        var dice = current.Single(x => x.MissionKey == "game_v1_weekly_repeat_dice");
        dice.Status.Should().Be("Completed");
        var fog = current.Single(x => x.MissionKey == "game_v1_weekly_monster_fog");
        fog.Status.Should().Be("Completed");
        var claimed = (await f.Service.ClaimAsync(1, fog.PlayerMissionId, default)).Single();
        (await f.Service.ClaimAsync(1, fog.PlayerMissionId, default)).Single().Should().BeEquivalentTo(claimed);
        var activation = f.Context.MissionActivations.Single(x => x.Id == fog.ActivationId);
        activation.EndsAt = DateTime.UtcNow.AddSeconds(-1); await f.Context.SaveChangesAsync();
        var history = await f.Service.HistoryAsync(1, 1, 30, default);
        var row = history.Single(x => x.PlayerMissionId == fog.PlayerMissionId);
        row.Status.Should().Be("Claimed"); row.ClaimedAt.Should().NotBeNull(); row.CompletedAt.Should().NotBeNull();
        row.GrantedRewards.Should().HaveCount(2);
        (await f.Service.HistoryAsync(2, 1, 30, default)).Should().BeEmpty();
        f.Context.Players.Single(x => x.Id == 101).Gold.Should().Be(50);
        f.Context.Players.Single(x => x.Id == 101).Experience.Should().Be(25);
    }

    [Fact]
    public async Task Seeded_answer_and_win_streaks_reset_on_wrong_answer_or_non_win()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.Service.SeedGameCatalogueAsync(40, default);
        var match = await f.Base.Matches.RegisterAsync(f.Base.Registration(), default);
        foreach (var kind in new[] { "CorrectAnswer", "CorrectAnswer", "WrongAnswer", "CorrectAnswer" })
        {
            var e = f.Event(kind); e.MatchId = match.MatchId; e.OccurredAt = DateTime.UtcNow;
            e.EventData = new() { ["questionType"] = JsonSerializer.SerializeToElement("MCQ"), ["fast"] = JsonSerializer.SerializeToElement(false) };
            await f.Service.EventsAsync(new() { e }, default);
        }
        foreach (var won in new[] { true, false, true })
        {
            var e = f.Event("MatchCompleted"); e.MatchId = match.MatchId; e.OccurredAt = DateTime.UtcNow;
            e.EventData = new() { ["won"] = JsonSerializer.SerializeToElement(won), ["matchType"] = JsonSerializer.SerializeToElement("Ranked") };
            await f.Service.EventsAsync(new() { e }, default);
        }
        var current = await f.Service.GetMyAsync(1, default);
        current.Single(x => x.MissionKey == "game_v1_weekly_answer_streak").CurrentProgress.Should().Be(1);
        current.Single(x => x.MissionKey == "game_v1_weekly_win_streak").CurrentProgress.Should().Be(1);
    }

    [Fact]
    public async Task History_validates_pagination_and_never_assigns_or_auto_claims()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.Service.SeedGameCatalogueAsync(40, default);
        (await f.Service.HistoryAsync(1, 1, 30, default)).Should().BeEmpty();
        f.Context.PlayerMissions.Should().BeEmpty();
        await Assert.ThrowsAsync<GenericException>(() => f.Service.HistoryAsync(1, 0, 30, default));
        await Assert.ThrowsAsync<GenericException>(() => f.Service.HistoryAsync(1, 1, 101, default));
        var missions = await f.Service.GetMyAsync(1, default);
        var m = f.Context.PlayerMissions.Single(x => x.Id == missions[0].PlayerMissionId);
        m.Status = PlayerMissionStatus.Completed; m.CompletedAt = DateTime.UtcNow;
        f.Context.MissionActivations.Single(x => x.Id == m.MissionActivationId).EndsAt = DateTime.UtcNow.AddSeconds(-1);
        await f.Context.SaveChangesAsync();
        (await f.Service.HistoryAsync(1, 1, 1, default)).Should().ContainSingle();
        f.Context.MissionClaimLogs.Should().BeEmpty();
        f.Context.Players.Single(x => x.Id == 101).Gold.Should().Be(0);
    }
}
