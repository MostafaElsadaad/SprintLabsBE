using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Requests.QuestionData;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataTypesTests
{
    [Theory]
    [InlineData("TrueOrFalse")]
    [InlineData("FillBlank")]
    [InlineData("Ordering")]
    [InlineData("MatchingPairs")]
    [InlineData("DragAndDrop")]
    public async Task Typed_response_round_trips_with_correctness_and_preserves_shared_ids(string type)
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = Question(type);
        await f.Service.PublishAsync(40, q, default);
        var response = Response(q);
        var saved = await f.Service.RecordAsync(response, default);
        Assert.True(saved.IsCorrect);
        f.Context.ChangeTracker.Clear();
        var read = Assert.Single((await f.Service.HistoryAsync(1, 101, 10, 1, 50, default)).Items);
        Assert.True(read.IsCorrect);
        Assert.Equal(q.QuestionId, read.QuestionId);
        if (type == "TrueOrFalse") Assert.False(read.SelectedAnswer);
        if (type == "FillBlank") Assert.Equal("  K  ", read.AnswerText);
        if (type == "Ordering") Assert.Equal(new[] { "k", "l", "m" }, read.Ordering.Select(x => x.ItemId));
        if (type == "MatchingPairs") Assert.Equal(new[] { "calcium", "phosphorus" }, read.Matching.Select(x => x.LeftPairId));
        if (type == "DragAndDrop") Assert.Equal(new[] { "nine-b", "nine-a" }, read.DragDrop.Select(x => x.SelectedOptionId));
    }

    [Theory]
    [InlineData("Ordering")]
    [InlineData("MatchingPairs")]
    [InlineData("DragAndDrop")]
    public async Task Duplicate_placements_or_right_selections_are_rejected_atomically(string type)
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = Question(type); await f.Service.PublishAsync(40, q, default);
        var r = Response(q);
        if (type == "Ordering") r.Ordering[1].SelectedPosition = 0;
        if (type == "MatchingPairs") r.Matching[1].SelectedRightPairId = r.Matching[0].SelectedRightPairId;
        if (type == "DragAndDrop") r.DragDrop[1].SelectedOptionId = r.DragDrop[0].SelectedOptionId;
        await Assert.ThrowsAsync<GenericException>(() => f.Service.RecordAsync(r, default));
        Assert.Equal(0, await f.Context.Set<QuestionHistory>().CountAsync());
    }

    [Fact]
    public async Task Child_identifier_collision_rolls_back_the_new_parent_question()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = Question("Ordering"); await f.Service.PublishAsync(40, q, default);
        f.Context.ChangeTracker.Clear();
        q.QuestionId = Guid.NewGuid();
        await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(40, q, default));
        f.Context.ChangeTracker.Clear();
        Assert.Equal(1, await f.Context.Set<Question>().CountAsync());
        Assert.Equal(3, await f.Context.Set<OrderingItem>().CountAsync());
    }

    internal static QuestionDataRequest Question(string type)
    {
        var q = new QuestionDataRequest { QuestionId = Guid.NewGuid(), Curriculum = "Egypt", Grade = 7,
            Language = "en", Subject = "Science", Term = 1, Unit = 1, Lesson = 1, QuestionType = type, QuestionText = "Source atom lesson question" };
        switch (type)
        {
            case "TrueOrFalse": q.CorrectAnswer = false; break;
            case "FillBlank": q.AcceptedAnswers = new() { "potassium", "K" }; break;
            case "Ordering": q.Items = new() { new() { ItemId = "k", ItemText = "K", CorrectPosition = 0 }, new() { ItemId = "m", ItemText = "M", CorrectPosition = 2 }, new() { ItemId = "l", ItemText = "L", CorrectPosition = 1 } }; break;
            case "MatchingPairs": q.Pairs = new() { new() { PairId = "phosphorus", LeftText = "Phosphorus", RightText = "Strong roots" }, new() { PairId = "calcium", LeftText = "Calcium carbonate", RightText = "Limestone" } }; break;
            case "DragAndDrop": q.Options = new() { new() { OptionId = "nine-a", OptionText = "9" }, new() { OptionId = "nine-b", OptionText = "9" }, new() { OptionId = "eight", OptionText = "8" } }; q.BlankAnswers = new() { new() { BlankNumber = 0, CorrectText = "9" }, new() { BlankNumber = 1, CorrectText = "9" } }; break;
        }
        return q;
    }

    internal static QuestionHistoryRequest Response(QuestionDataRequest q)
    {
        var r = new QuestionHistoryRequest { QuestionId = q.QuestionId, PlayerId = 101, MatchId = 10, TimeTakenSeconds = 1.2m };
        switch (q.QuestionType)
        {
            case "TrueOrFalse": r.SelectedAnswer = false; break;
            case "FillBlank": r.AnswerText = "  K  "; break;
            case "Ordering": r.Ordering = new() { new() { ItemId = "k", SelectedPosition = 0 }, new() { ItemId = "m", SelectedPosition = 2 }, new() { ItemId = "l", SelectedPosition = 1 } }; break;
            case "MatchingPairs": r.Matching = new() { new() { LeftPairId = "phosphorus", SelectedRightPairId = "phosphorus" }, new() { LeftPairId = "calcium", SelectedRightPairId = "calcium" } }; break;
            case "DragAndDrop": r.DragDrop = new() { new() { BlankNumber = 0, SelectedOptionId = "nine-b" }, new() { BlankNumber = 1, SelectedOptionId = "nine-a" } }; break;
        }
        return r;
    }
}
