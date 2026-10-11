using Domain.Models.QuestionData;
using Shared.Requests.QuestionData;
using Shared.Responses;
using System.Globalization;

namespace Infrastructure.Services;

internal static class QuestionDataMapping
{
    public static QuestionDataRequest Question(Question q) => new()
    {
        QuestionId = q.QuestionId, Curriculum = q.Curriculum, Grade = q.Grade, Language = q.Language,
        Subject = q.Subject, Term = q.Term, Unit = q.Unit, Lesson = q.Lesson,
        QuestionType = q.QuestionType.ToString(), QuestionText = q.QuestionText, TimerSeconds = CanonicalSeconds(q.TimerSeconds),
        Choices = q.Choices.OrderBy(x => x.ChoiceId, StringComparer.Ordinal).Select(x => new QuestionChoiceData { ChoiceId = x.ChoiceId, ChoiceText = x.ChoiceText, IsCorrect = x.IsCorrect }).ToList(),
        CorrectAnswer = q.BooleanAnswers.SingleOrDefault()?.CorrectAnswer,
        AcceptedAnswers = q.AcceptedAnswers.Select(x => x.AcceptedAnswer).OrderBy(x => x, StringComparer.Ordinal).ToList(),
        Items = q.Items.OrderBy(x => x.ItemId, StringComparer.Ordinal).Select(x => new QuestionOrderingData { ItemId = x.ItemId, ItemText = x.ItemText, CorrectPosition = x.CorrectPosition }).ToList(),
        Pairs = q.Pairs.OrderBy(x => x.PairId, StringComparer.Ordinal).Select(x => new QuestionPairData { PairId = x.PairId, LeftText = x.LeftText, RightText = x.RightText }).ToList(),
        Options = q.Options.OrderBy(x => x.OptionId, StringComparer.Ordinal).Select(x => new QuestionOptionData { OptionId = x.OptionId, OptionText = x.OptionText }).ToList(),
        BlankAnswers = q.BlankAnswers.OrderBy(x => x.BlankNumber).Select(x => new QuestionBlankData { BlankNumber = x.BlankNumber, CorrectText = x.CorrectText }).ToList()
    };

    public static QuestionHistoryResponse History(QuestionHistory h) => new()
    {
        HistoryId = h.HistoryId, PlayerId = h.PlayerId, QuestionId = h.QuestionId, MatchId = h.MatchId, TimeTakenSeconds = CanonicalSeconds(h.TimeTakenSeconds),
        SelectedChoiceId = h.MCQHistoryRows.SingleOrDefault()?.SelectedChoiceId,
        SelectedAnswer = h.TrueFalseHistoryRows.SingleOrDefault()?.SelectedAnswer,
        AnswerText = h.FillBlankHistoryRows.SingleOrDefault()?.AnswerText,
        Ordering = h.OrderingHistoryRows.OrderBy(x => x.SelectedPosition).Select(x => new OrderingResponseData { ItemId = x.ItemId, SelectedPosition = x.SelectedPosition }).ToList(),
        Matching = h.MatchingHistoryRows.OrderBy(x => x.LeftPairId, StringComparer.Ordinal).Select(x => new MatchingResponseData { LeftPairId = x.LeftPairId, SelectedRightPairId = x.SelectedRightPairId }).ToList(),
        DragDrop = h.DragDropHistoryRows.OrderBy(x => x.BlankNumber).Select(x => new DragDropResponseData { BlankNumber = x.BlankNumber, SelectedOptionId = x.SelectedOptionId }).ToList()
    };

    // Keep equivalent decimal scales (10, 10.0, 10.000) equal for immutable publication/replay.
    private static decimal CanonicalSeconds(decimal value)
        => decimal.Parse(value.ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
