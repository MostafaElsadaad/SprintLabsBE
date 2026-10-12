using Domain.Enums;
using Domain.Models.QuestionData;
using Shared.Requests.QuestionData;

namespace Infrastructure.Services;

internal static class QuestionDataValidation
{
    public static Question Question(QuestionDataRequest r)
    {
        if (r == null || r.QuestionId == Guid.Empty) throw QuestionDataService.Error("A shared question UUID is required.");
        Text(r.Curriculum, 120); Text(r.Language, 20); Text(r.Subject, 120); Text(r.QuestionText, 16000);
        Seconds(r.TimerSeconds, allowZero: false);
        if (r.Grade <= 0 || r.Term <= 0 || r.Unit <= 0 || r.Lesson <= 0) throw QuestionDataService.Error("Grade, term, unit and lesson must be positive.");
        if (!Enum.TryParse<QuestionType>(r.QuestionType, out var type) || !Enum.IsDefined(type) || type.ToString() != r.QuestionType)
            throw QuestionDataService.Error("Unsupported question type.");
        if (r.Choices == null || r.AcceptedAnswers == null || r.Items == null || r.Pairs == null || r.Options == null || r.BlankAnswers == null)
            throw QuestionDataService.Error("Question collections cannot be null.");
        var groups = new[] { r.Choices.Count > 0, r.CorrectAnswer.HasValue, r.AcceptedAnswers.Count > 0,
            r.Items.Count > 0, r.Pairs.Count > 0, r.Options.Count > 0 || r.BlankAnswers.Count > 0 };
        if (groups.Count(x => x) != 1 || !groups[(int)type]) throw QuestionDataService.Error("Supply exactly the answer data for the question type.");
        var q = new Question { QuestionId = r.QuestionId, Curriculum = r.Curriculum, Grade = r.Grade,
            Language = r.Language, Subject = r.Subject, Term = r.Term, Unit = r.Unit, Lesson = r.Lesson,
            QuestionType = type, QuestionText = r.QuestionText, TimerSeconds = r.TimerSeconds };
        switch (type)
        {
            case QuestionType.MCQ:
                if (r.Choices.Count < 2 || r.Choices.Count(x => x?.IsCorrect == true) != 1 || r.Choices.Any(x => x == null || x.IsCorrect == null))
                    throw QuestionDataService.Error("MCQ requires at least two choices and exactly one correct choice.");
                Ids(r.Choices.Select(x => x.ChoiceId));
                foreach (var x in r.Choices) { Text(x.ChoiceText, 4000); q.Choices.Add(new() { QuestionId = q.QuestionId, ChoiceId = x.ChoiceId, ChoiceText = x.ChoiceText, IsCorrect = x.IsCorrect!.Value }); }
                break;
            case QuestionType.TrueOrFalse:
                q.BooleanAnswers.Add(new() { QuestionId = q.QuestionId, CorrectAnswer = r.CorrectAnswer!.Value });
                break;
            case QuestionType.FillBlank:
                if (r.AcceptedAnswers.Count > 100 || r.AcceptedAnswers.Select(Normalize).Distinct(StringComparer.Ordinal).Count() != r.AcceptedAnswers.Count)
                    throw QuestionDataService.Error("Accepted answers must be distinct and limited to 100 variants.");
                foreach (var x in r.AcceptedAnswers) { Text(x, 255); q.AcceptedAnswers.Add(new() { QuestionId = q.QuestionId, AcceptedAnswer = x }); }
                break;
            case QuestionType.Ordering:
                if (r.Items.Any(x => x == null || x.CorrectPosition == null)) throw QuestionDataService.Error("Ordering requires a correct position for every item.");
                Ids(r.Items.Select(x => x.ItemId)); Positions(r.Items.Select(x => x.CorrectPosition!.Value), r.Items.Count);
                foreach (var x in r.Items) { Text(x.ItemText, 4000); q.Items.Add(new() { QuestionId = q.QuestionId, ItemId = x.ItemId, ItemText = x.ItemText, CorrectPosition = x.CorrectPosition!.Value }); }
                break;
            case QuestionType.MatchingPairs:
                if (r.Pairs.Any(x => x == null)) throw QuestionDataService.Error("Matching pairs cannot be null.");
                Ids(r.Pairs.Select(x => x.PairId));
                foreach (var x in r.Pairs) { Text(x.LeftText, 4000); Text(x.RightText, 4000); q.Pairs.Add(new() { QuestionId = q.QuestionId, PairId = x.PairId, LeftText = x.LeftText, RightText = x.RightText }); }
                break;
            case QuestionType.DragAndDrop:
                if (r.Options.Any(x => x == null) || r.BlankAnswers.Any(x => x == null)) throw QuestionDataService.Error("Drag/drop entries cannot be null.");
                Ids(r.Options.Select(x => x.OptionId)); Positions(r.BlankAnswers.Select(x => x.BlankNumber), r.BlankAnswers.Count);
                if (r.BlankAnswers.Count > r.Options.Count) throw QuestionDataService.Error("Supply enough option instances for all blanks.");
                foreach (var x in r.Options) { Text(x.OptionText, 4000); q.Options.Add(new() { QuestionId = q.QuestionId, OptionId = x.OptionId, OptionText = x.OptionText }); }
                var available = r.Options.GroupBy(x => x.OptionText, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
                foreach (var x in r.BlankAnswers)
                {
                    Text(x.CorrectText, 4000);
                    if (!available.TryGetValue(x.CorrectText, out var count) || count == 0) throw QuestionDataService.Error("Each correct blank must have an available option instance.");
                    available[x.CorrectText]--;
                    q.BlankAnswers.Add(new() { QuestionId = q.QuestionId, BlankNumber = x.BlankNumber, CorrectText = x.CorrectText });
                }
                break;
        }
        return q;
    }

    public static void Text(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max) throw QuestionDataService.Error($"Required text must contain 1..{max} characters.");
    }
    public static void Ids(IEnumerable<string> values)
    {
        var ids = values.ToList();
        if (ids.Count is < 1 or > 100 || ids.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 80 || x.Any(c => c < 33 || c > 126)) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
            throw QuestionDataService.Error("Supply 1..100 distinct stable ASCII item identifiers, at most 80 characters each.");
    }
    public static void Positions(IEnumerable<int> values, int count)
    {
        if (count is < 1 or > 100 || !values.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, count)))
            throw QuestionDataService.Error("Positions must cover every item or blank consecutively from zero.");
    }
    public static string Normalize(string? text) => (text ?? string.Empty).Trim().ToUpperInvariant();
    public static void Seconds(decimal value, bool allowZero)
    {
        if (value < 0 || (!allowZero && value == 0) || value > 9999999.999m || decimal.Round(value, 3) != value)
            throw QuestionDataService.Error("Time must be in seconds with at most three decimal places and within decimal(10,3) range.");
    }
}
