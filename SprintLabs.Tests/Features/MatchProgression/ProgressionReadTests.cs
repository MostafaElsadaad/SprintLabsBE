using System.Net;
using Domain.Enums;
using FluentAssertions;
using Shared.Exceptions;
using Shared.Requests;

namespace Compass.Tests.Features.MatchProgression;

public class ProgressionReadTests
{
    [Fact]
    public async Task Self_and_staff_progression_are_identity_and_class_scoped()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        (await f.Reads.GetProgressionAsync(1, null, default)).PlayerProfileId.Should().Be(101);
        (await f.Reads.GetProgressionAsync(20, 101, default)).PlayerProfileId.Should().Be(101);
        (await f.Reads.GetRankingAsync(10, 102, default)).Rp.Should().Be(1000);
        (await f.Reads.GetRankingAsync(40, 103, default)).PlayerProfileId.Should().Be(103);
        foreach (var (caller, target) in new[] { (1L, 102L), (20L, 102L), (10L, 103L), (30L, 101L) })
            (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetProgressionAsync(caller, target, default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        f.Context.Users.Single(x => x.Id == 1).Status = UserStatus.Suspended;
        await f.Context.SaveChangesAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetRankingAsync(1, null, default))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Global_leaderboard_excludes_suspended_and_uses_stable_ties_and_pagination()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var first = await f.Reads.GetLeaderboardAsync(1, null, new() { PageSize = 1 }, default);
        first.Total.Should().Be(3); first.Items.Single().PlayerProfileId.Should().Be(102);
        var second = await f.Reads.GetLeaderboardAsync(1, null, new() { Page = 2, PageSize = 1 }, default);
        second.Items.Single().PlayerProfileId.Should().Be(103); second.Items.Single().Position.Should().Be(2);
        f.Context.Users.Single(x => x.Id == 2).Status = UserStatus.Suspended;
        await f.Context.SaveChangesAsync();
        var filtered = await f.Reads.GetLeaderboardAsync(1, null, new(), default);
        filtered.Total.Should().Be(2); filtered.Items.Should().NotContain(x => x.PlayerProfileId == 102);
        (await f.Reads.GetLeaderboardAsync(1, null, new() { Page = 99 }, default)).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Community_leaderboard_respects_assignments_filters_revoked_and_archived()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        (await f.Reads.GetLeaderboardAsync(10, 1, new(), default)).Items.Should().HaveCount(2);
        (await f.Reads.GetLeaderboardAsync(20, 1, new(), default)).Items.Single().PlayerProfileId.Should().Be(101);
        (await f.Reads.GetLeaderboardAsync(10, 1, new() { GradeId = 1, ClassId = 2 }, default)).Items.Single().PlayerProfileId.Should().Be(102);
        (await f.Reads.GetLeaderboardAsync(20, 1, new() { ClassId = 2 }, default)).Items.Should().BeEmpty();
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetLeaderboardAsync(30, 1, new(), default))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetLeaderboardAsync(10, 1, new() { ClassId = 3 }, default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        f.Context.StudentLicenses.Single(x => x.PlayerProfileId == 101).Status = StudentLicenseStatus.Revoked;
        f.Context.Classes.Single(x => x.Id == 2).Status = ClassStatus.Deleted;
        await f.Context.SaveChangesAsync();
        (await f.Reads.GetLeaderboardAsync(10, 1, new(), default)).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task Paging_is_bounded(int page, int size)
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetLeaderboardAsync(1, null,
            new() { Page = page, PageSize = size }, default))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task History_and_detail_never_expose_other_players_educational_answers_or_other_classes()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(communityId: 1), default);
        await f.Matches.CompleteAsync(match.MatchId, f.Completion(communityId: 1), default);
        var self = await f.Reads.GetHistoryAsync(1, null, null, new(), default);
        self.Total.Should().Be(1); self.Items.Single().Reward!.XpGained.Should().Be(82);
        var detail = await f.Reads.GetMatchAsync(2, match.MatchId, default);
        detail.Players.Should().HaveCount(2);
        detail.QuestionResults.Should().BeEmpty();
        detail.Players.Single(x => x.PlayerProfileId == 101).Reward.Should().BeNull();
        detail.Players.Single(x => x.PlayerProfileId == 101).CorrectAnswers.Should().BeNull();
        var teacher = await f.Reads.GetMatchAsync(20, match.MatchId, default);
        teacher.Players.Single().PlayerProfileId.Should().Be(101);
        teacher.QuestionResults.Should().HaveCount(4);
        (await f.Reads.GetMatchAsync(40, match.MatchId, default)).Players.Should().HaveCount(2);
        (await f.Reads.GetHistoryAsync(20, 1, null, new(), default)).Total.Should().Be(1);
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetHistoryAsync(20, 1, 102, new(), default))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetMatchAsync(3, match.MatchId, default))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Assert.ThrowsAsync<GenericException>(() => f.Reads.GetHistoryAsync(10, 2, null, new(), default))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Empty_history_and_multi_level_progression_have_consistent_values()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        (await f.Reads.GetHistoryAsync(1, null, null, new(), default)).Total.Should().Be(0);
        f.Context.Players.Single(x => x.Id == 101).Experience = 10000;
        await f.Context.SaveChangesAsync();
        var progression = await f.Reads.GetProgressionAsync(1, null, default);
        progression.Level.Should().BeGreaterThan(3);
        progression.XpIntoCurrentLevel.Should().BeInRange(0, progression.NextLevelXpRequirement - 1);
    }

    [Fact]
    public async Task Owners_retain_school_history_after_enrollment_removal_or_class_archive()
    {
        using var f = new ProgressionFixture(); await f.SeedAsync();
        var match = await f.Matches.RegisterAsync(f.Registration(communityId: 1), default);
        await f.Matches.CompleteAsync(match.MatchId, f.Completion(communityId: 1), default);
        foreach (var license in f.Context.StudentLicenses.Where(x => x.CommunityId == 1)) license.Status = StudentLicenseStatus.Revoked;
        foreach (var row in f.Context.Classes.Where(x => x.CommunityId == 1)) row.Status = ClassStatus.Deleted;
        await f.Context.SaveChangesAsync();
        (await f.Reads.GetHistoryAsync(10, 1, null, new(), default)).Total.Should().Be(1);
        (await f.Reads.GetHistoryAsync(10, 1, 101, new(), default)).Total.Should().Be(1);
        (await f.Reads.GetMatchAsync(10, match.MatchId, default)).Players.Should().HaveCount(2);
        (await f.Reads.GetHistoryAsync(20, 1, null, new(), default)).Total.Should().Be(0);
    }
}
