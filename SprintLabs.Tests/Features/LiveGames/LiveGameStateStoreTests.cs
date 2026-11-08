using FluentAssertions;
using Infrastructure.Services;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Compass.Tests.Features.LiveGames;

public class LiveGameStateStoreTests
{
    private static int sequence;

    private static LiveGameEventRequest Event(string type, long? player = null, string? name = null,
        bool? correct = null, int? score = null, int? question = null, int? rank = null)
        => new()
        {
            EventId = "e-" + Interlocked.Increment(ref sequence),
            Type = type,
            OccurredAt = DateTime.UtcNow,
            PlayerProfileId = player,
            DisplayName = name,
            IsCorrect = correct,
            Score = score,
            QuestionNumber = question,
            Rank = rank
        };

    [Fact]
    public void Folds_a_full_game_into_a_ranked_live_view()
    {
        var store = new LiveGameStateStore();
        store.Apply(1, Event("player_joined", 10, "Mona")).Should().BeTrue();
        store.Apply(1, Event("player_joined", 20, "Omar")).Should().BeTrue();
        store.JoinedCount(1).Should().Be(2);

        store.Apply(1, Event("match_started"));
        store.Apply(1, Event("question_opened", question: 1));
        store.Apply(1, Event("answer_graded", 20, correct: true, score: 10));
        store.Apply(1, Event("answer_graded", 10, correct: false));

        var live = new LiveGameResponse();
        store.Fill(1, live);
        live.QuestionsAsked.Should().Be(1);
        live.CurrentQuestion!.CorrectCount.Should().Be(1);
        live.CurrentQuestion.WrongCount.Should().Be(1);
        live.Players.Select(p => p.DisplayName).Should().Equal("Omar", "Mona");
        live.Players[0].CurrentAnswer.Should().Be("correct");
        live.Players[1].CurrentAnswer.Should().Be("wrong");

        store.Apply(1, Event("question_opened", question: 2));
        var next = new LiveGameResponse();
        store.Fill(1, next);
        next.Players.Should().OnlyContain(p => p.CurrentAnswer == null);

        store.Apply(1, Event("player_result", 10, score: 30, rank: 1));
        store.Apply(1, Event("player_result", 20, score: 10, rank: 2));
        store.Apply(1, Event("match_ended"));
        var standings = store.Standings(1);
        standings.Select(p => (p.DisplayName, p.Rank, p.Score)).Should().Equal(("Mona", 1, 30), ("Omar", 2, 10));
    }

    [Fact]
    public void Ignores_duplicate_and_unknown_events()
    {
        var store = new LiveGameStateStore();
        var joined = Event("player_joined", 5, "Sara");
        store.Apply(7, joined).Should().BeTrue();
        store.Apply(7, joined).Should().BeFalse();
        store.Apply(7, Event("something_else")).Should().BeFalse();
        store.Apply(7, Event("answer_graded", 99, correct: true)).Should().BeFalse();
        store.JoinedCount(7).Should().Be(1);
    }

    [Fact]
    public void A_lobby_leave_removes_the_player_but_a_running_leave_keeps_their_score()
    {
        var store = new LiveGameStateStore();
        store.Apply(3, Event("player_joined", 1, "A"));
        store.Apply(3, Event("player_left", 1));
        store.Standings(3).Should().BeEmpty();

        store.Apply(3, Event("player_joined", 2, "B"));
        store.Apply(3, Event("match_started"));
        store.Apply(3, Event("score_changed", 2, score: 40));
        store.Apply(3, Event("player_left", 2));
        store.Standings(3).Should().ContainSingle(p => p.Score == 40 && !p.Connected);
        store.JoinedCount(3).Should().Be(0);
    }
}
