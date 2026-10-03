using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Shared.Exceptions;
using Shared.Requests;
using System.Net;
using System.Text.Json;
namespace Compass.Tests.Features.Missions;
public class MissionWorkflowTests
{
    [Fact]
    public async Task Stable_fixed_and_weighted_random_assignment_is_once_per_player_and_activation()
    {
        using var f = new MissionFixture(); await f.SeedAsync();
        var ids = new List<long>();
        for (var i = 0; i < 5; i++) ids.Add(await f.Service.CreateTemplateAsync(40, new() { MissionKey = $"random{i}", Title = "Random", Category = "Progress",
            PeriodType = "Open", EventType = "CorrectAnswer", ProgressType = "Count", TargetValue = 2,
            Rewards = new() { new() { RewardType = "XP", Amount = 50 } } }, default));
        await f.Service.CreateActivationAsync(40, new() { Name = "Random period", PeriodType = "Open", StartsAt = DateTime.UtcNow.AddDays(-1), RandomMissionCount = 2,
            Items = ids.Select((id, i) => new MissionActivationItemRequest { MissionTemplateId = id, AssignmentMode = i < 2 ? "Fixed" : "RandomPool", Weight = i + 1 }).ToList() }, default);
        var first = await f.Service.GetMyAsync(1, default);
        (await f.Service.GetMyAsync(1, default)).Should().BeEquivalentTo(first);
        first.Should().HaveCount(4); first.Select(x => x.MissionKey).Should().Contain(new[] { "random0", "random1" });
        (await f.Service.GetMyAsync(2, default)).Should().HaveCount(4);
        f.Context.PlayerMissionAssignments.Count().Should().Be(2); f.Context.PlayerMissions.Count().Should().Be(8);
    }
    [Theory]
    [InlineData("Count",2)] [InlineData("Boolean",1)] [InlineData("MaxValue",2)] [InlineData("Streak",2)] [InlineData("UniqueCount",2)]
    public async Task Five_progress_modes_complete_and_clamp(string mode, int target)
    {
        using var f = new MissionFixture(); await f.SeedAsync(); var m = await f.AssignAsync(mode, target);
        await f.Service.EventsAsync(new() { f.Event(value: 1), f.Event(value: 2), f.Event(value: 20) }, default);
        var current = (await f.Service.GetMyAsync(1, default)).Single();
        current.Status.Should().Be("Completed"); current.CurrentProgress.Should().Be(target);
        (await f.Service.ClaimAsync(1, m.PlayerMissionId, default)).Single().NewXp.Should().Be(50);
        f.Context.Players.Single(x => x.Id == 101).Gold.Should().Be(20);
    }
    [Fact]
    public async Task Event_replay_is_saved_once_and_changed_retry_conflicts()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync(); var e = f.Event();
        var first = (await f.Service.EventsAsync(new() { e, e }, default));
        first[1].Duplicate.Should().BeTrue(); f.Context.MissionEventLogs.Count().Should().Be(1);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(1);
        e.EventData["value"] = JsonSerializer.SerializeToElement(99);
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.EventsAsync(new() { e }, default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Conditions_and_unique_values_filter_without_duplicate_progress()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("UniqueCount", conditions: new() { ["unitId"] = JsonSerializer.SerializeToElement(1) });
        var ignored = f.Event(value: "a"); ignored.EventData["unitId"] = JsonSerializer.SerializeToElement(2);
        await f.Service.EventsAsync(new() { ignored }, default);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(0);
        var first = f.Event(value: "a"); first.EventData["unitId"] = JsonSerializer.SerializeToElement(1);
        var repeat = f.Event(value: "a"); repeat.EventData["unitId"] = JsonSerializer.SerializeToElement(1);
        await f.Service.EventsAsync(new() { first, repeat }, default);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(1);
    }
    [Fact]
    public async Task Streak_resets_on_loss_and_rejects_out_of_order_facts()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("Streak", 3, "MonsterDefeated", reset: "MonsterFightLost");
        var first = f.Event("MonsterDefeated"); var loss = f.Event("MonsterFightLost"); var next = f.Event("MonsterDefeated");
        await f.Service.EventsAsync(new() { first, loss, next }, default);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(1);
        var old = f.Event("MonsterDefeated"); old.OccurredAt = first.OccurredAt.AddSeconds(-1);
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.EventsAsync(new() { old }, default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Dice_repeat_streak_restarts_when_value_changes()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("Streak", 2, "DiceRolled", repeat: true);
        await f.Service.EventsAsync(new() { f.Event("DiceRolled", 2), f.Event("DiceRolled", 3) }, default);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(1);
        await f.Service.EventsAsync(new() { f.Event("DiceRolled", 3) }, default);
        (await f.Service.GetMyAsync(1, default)).Single().Status.Should().Be("Completed");
    }
    [Fact]
    public async Task Match_scope_and_historical_roster_are_validated()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("Count", 3, matchScoped: true);
        var a = await f.Base.Matches.RegisterAsync(f.Base.Registration(), default); var b = await f.Base.Matches.RegisterAsync(f.Base.Registration(), default);
        var e = f.Event(); e.MatchId = a.MatchId; await f.Service.EventsAsync(new() { e }, default);
        var second = f.Event(); second.MatchId = b.MatchId; await f.Service.EventsAsync(new() { second }, default);
        (await f.Service.GetMyAsync(1, default)).Single().CurrentProgress.Should().Be(1);
        second.EventId = Guid.NewGuid().ToString(); second.PlayerProfileId = 103;
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.EventsAsync(new() { second }, default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Claims_replay_reward_snapshot_and_do_not_change_rp_or_match_rewards()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() }, default);
        f.Context.MissionRewards.Single(x => x.RewardType == MissionRewardType.XP).Amount = 999; await f.Context.SaveChangesAsync();
        var claimed = (await f.Service.ClaimAsync(1, m.PlayerMissionId, default)).Single();
        (await f.Service.ClaimAsync(1, m.PlayerMissionId, default)).Single().Should().BeEquivalentTo(claimed);
        claimed.NewXp.Should().Be(50); f.Context.PlayerXpLogs.Count().Should().Be(1); f.Context.MissionClaimLogs.Count().Should().Be(2);
        f.Context.Players.Single(x => x.Id == 101).Rp.Should().Be(450); f.Context.MatchRewardResults.Should().BeEmpty();
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.ClaimAsync(2, m.PlayerMissionId, default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Lazy_expiry_autoclaims_only_if_configured_and_reset_is_repeat_safe(bool auto)
    {
        using var f = new MissionFixture(); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() }, default);
        var a = f.Context.MissionActivations.Single(); a.EndsAt = DateTime.UtcNow.AddSeconds(-1); a.AutoClaimCompletedOnReset = auto; await f.Context.SaveChangesAsync();
        (await f.Service.GetMyAsync(1, default)).Should().BeEmpty();
        f.Context.PlayerMissions.Single().Status.Should().Be(auto ? PlayerMissionStatus.AutoClaimed : PlayerMissionStatus.Expired);
        (await f.Service.ResetAsync(40, default)).EndedActivations.Should().Be(1);
        (await f.Service.ResetAsync(40, default)).AutoClaimedMissions.Should().Be(0);
        f.Context.Players.Single(x => x.Id == 101).Experience.Should().Be(auto ? 50 : 0);
    }
    [Fact]
    public async Task Incomplete_missions_expire_and_suspended_profiles_settle_only_via_admin_reset()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync();
        f.Context.MissionActivations.Single().EndsAt = DateTime.UtcNow.AddSeconds(-1);
        f.Context.Users.Single(x => x.Id == 1).Status = UserStatus.Suspended; await f.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<GenericException>(() => f.Service.GetMyAsync(1, default));
        (await f.Service.ResetAsync(40, default)).ExpiredMissions.Should().Be(1);
        f.Context.MissionClaimLogs.Should().BeEmpty();
    }
    [Fact]
    public async Task Seed_is_repeat_safe_and_boxes_open_immediately_with_saved_rolls()
    {
        using var f = new MissionFixture(); await f.SeedAsync();
        var ids = await f.Service.SeedAsync(40, default);
        (await f.Service.SeedAsync(40, default)).Should().Equal(ids);
        (await f.Service.GetMyAsync(1, default)).Should().HaveCount(4);
        var events = Enumerable.Range(0,3).Select(_ => { var e = f.Event("MatchWon"); e.EventData["position"] = JsonSerializer.SerializeToElement(1); return e; }).ToList();
        await f.Service.EventsAsync(events, default);
        var m = (await f.Service.GetMyAsync(1, default)).Single(x => x.MissionKey == "seed_win_3");
        var claimed = (await f.Service.ClaimAsync(1, m.PlayerMissionId, default)).Single();
        claimed.Rewards.Single().RewardItem!.QuantityOwned.Should().Be(1);
        f.Context.PlayerInventoryItems.Single().Quantity.Should().Be(1);
        (await f.Service.ClaimAsync(1, m.PlayerMissionId, default)).Single().Should().BeEquivalentTo(claimed);
        f.Context.PlayerInventoryItems.Single().Quantity.Should().Be(1);
    }
    [Fact]
    public async Task Claim_all_expiration_with_autoclaim_disabled_commits_expiry_without_claim_error()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("Boolean",1);
        await f.Service.EventsAsync(new() { f.Event() }, default);
        var a = f.Context.MissionActivations.Single(); a.EndsAt = DateTime.UtcNow.AddSeconds(-1); a.AutoClaimCompletedOnReset = false;
        await f.Context.SaveChangesAsync();
        (await f.Service.ClaimAsync(1,null,default)).Should().BeEmpty();
        f.Context.PlayerMissions.Single().Status.Should().Be(PlayerMissionStatus.Expired);
        f.Context.Players.Single(x => x.Id == 101).Experience.Should().Be(0);
    }
    [Fact]
    public async Task Future_and_ended_activations_do_not_assign_and_period_successor_assigns_new_missions()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync();
        var a = f.Context.MissionActivations.Single(); a.StartsAt = DateTime.UtcNow.AddDays(1); await f.Context.SaveChangesAsync();
        (await f.Service.GetMyAsync(2,default)).Should().BeEmpty();
        a.StartsAt = DateTime.UtcNow.AddDays(-1); a.EndsAt = DateTime.UtcNow.AddSeconds(-1); await f.Context.SaveChangesAsync();
        (await f.Service.GetMyAsync(2,default)).Should().BeEmpty();
        var template = f.Context.MissionTemplates.Single();
        await f.Service.CreateActivationAsync(40,new() { Name = "Next period", PeriodType = "Open", StartsAt = DateTime.UtcNow.AddMinutes(-1),
            Items = new() { new() { MissionTemplateId = template.Id } } },default);
        (await f.Service.GetMyAsync(1,default)).Should().HaveCount(1);
        f.Context.PlayerMissions.Count().Should().Be(2);
    }
    [Fact]
    public async Task Rejected_or_incomplete_claim_and_overflow_do_not_grant_rewards()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); var m = await f.AssignAsync("Boolean",1);
        await Assert.ThrowsAsync<GenericException>(() => f.Service.ClaimAsync(1,m.PlayerMissionId,default));
        f.Context.MissionClaimLogs.Should().BeEmpty();
        await f.Service.EventsAsync(new() { f.Event() },default);
        f.Context.Players.Single(x => x.Id == 101).Experience = int.MaxValue; await f.Context.SaveChangesAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.ClaimAsync(1,m.PlayerMissionId,default))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        f.Context.MissionClaimLogs.Should().BeEmpty();
    }
    [Fact]
    public async Task Admin_configuration_is_platform_scoped_and_rejects_invalid_periods()
    {
        using var f = new MissionFixture(); await f.SeedAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.SeedAsync(10, default))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Assert.ThrowsAsync<GenericException>(() => f.Service.CreateActivationAsync(40, new() { Name = "Invalid", PeriodType = "Weekly", StartsAt = DateTime.UtcNow }, default));
        f.Context.MissionActivations.Should().BeEmpty();
    }
    [Fact]
    public async Task Invalid_value_payload_returns_controlled_bad_request()
    {
        using var f = new MissionFixture(); await f.SeedAsync(); await f.AssignAsync("MaxValue");
        (await Assert.ThrowsAsync<GenericException>(() => f.Service.EventsAsync(new() { f.Event(value: "bad") }, default))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
