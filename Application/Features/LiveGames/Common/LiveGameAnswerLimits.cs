using Shared.Requests.QuestionData;

namespace Application.Features.LiveGames.Common;

/// <summary>
/// How many answers each question type can have so the game can show all of them. Measured on the Unity
/// question screens: MCQ has four answer buttons, Ordering rows become unreadable past six, Matching pairs run
/// into the question past five, and Drag and drop has room for four answer chips. Fill in the blank answers are
/// typed, not shown, so its limit only keeps the list reasonable.
/// </summary>
public static class LiveGameAnswerLimits
{
    public const int McqChoices = 4;
    public const int OrderingItems = 6;
    public const int MatchingPairs = 5;
    public const int DragAndDropOptions = 4;
    public const int FillBlankAnswers = 10;

    /// <summary>A message describing the first limit the question breaks, or null when it fits the game.</summary>
    public static string? Violation(QuestionDataRequest question)
    {
        switch ((question.QuestionType ?? "").Trim())
        {
            case "MCQ":
                return Over(question.Choices.Count, McqChoices, "choices");
            case "Ordering":
                return Over(question.Items.Count, OrderingItems, "items to order");
            case "MatchingPairs":
                return Over(question.Pairs.Count, MatchingPairs, "pairs");
            case "DragAndDrop":
                return Over(question.Options.Count, DragAndDropOptions, "word options (correct words plus distractors)");
            case "FillBlank":
            case "FillInBlank":
                return Over(question.AcceptedAnswers.Count, FillBlankAnswers, "accepted answers");
            default:
                return null;
        }
    }

    public static bool Fits(QuestionDataRequest question) => Violation(question) == null;

    private static string? Over(int count, int limit, string what)
        => count > limit ? $"Use at most {limit} {what}; the game cannot show more (this question has {count})." : null;
}
