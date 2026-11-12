using System.Net;
using System.Text.Json;
using Domain.Services;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.LiveGames;
using Shared.Requests.QuestionData;

namespace Application.Features.LiveGames.Common;

/// <summary>
/// Validates and normalizes an ordered question list for a game or saved set: every question must have
/// a correct answer, duplicates are rejected, and a library question that was changed becomes a
/// game-only "edited" copy (the shared original is never modified).
/// </summary>
internal static class LiveGameQuestionNormalizer
{
    public const int MaxQuestions = 30;
    public const string Library = "library";
    public const string Edited = "edited";
    public const string Mine = "mine";
    public static readonly string[] ThinkingSkills = ["Remember", "Understand", "Apply", "Analyze", "Evaluate", "Create"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<List<LiveGameQuestionItem>> NormalizeAsync(
        IQuestionDataService questions,
        List<LiveGameQuestionItem>? items,
        CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0) throw BadRequest("Add at least one question.");
        if (items.Count > MaxQuestions) throw BadRequest($"A game can have at most {MaxQuestions} questions.");

        var libraryIds = items
            .Where(i => i?.Question != null && string.Equals(i.Source, Library, StringComparison.OrdinalIgnoreCase) && i.Question.QuestionId != Guid.Empty)
            .Select(i => i.Question.QuestionId)
            .ToList();
        var originals = (await questions.GetQuestionsByIdAsync(libraryIds, cancellationToken))
            .ToDictionary(q => q.QuestionId);

        var result = new List<LiveGameQuestionItem>(items.Count);
        var seenIds = new HashSet<Guid>();
        var seenTexts = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int number = i + 1;
            if (item?.Question == null) throw BadRequest($"Question {number} is empty.");
            string source = NormalizeSource(item.Source) ?? throw BadRequest($"Question {number} has an unknown source.");

            var question = Clone(item.Question);
            if (source != Library && question.QuestionId == Guid.Empty) question.QuestionId = Guid.NewGuid();

            QuestionDataRequest normalized;
            try
            {
                normalized = questions.NormalizeQuestion(question);
            }
            catch (GenericException exception)
            {
                throw BadRequest($"Question {number}: {exception.Message}");
            }

            if (source == Library &&
                (!originals.TryGetValue(normalized.QuestionId, out var original) || Json(original) != Json(normalized)))
            {
                source = Edited;
            }

            if (!seenIds.Add(normalized.QuestionId)) throw BadRequest($"Question {number} is already in this game.");
            if (!seenTexts.Add(normalized.QuestionType + "|" + normalized.QuestionText.Trim().ToUpperInvariant()))
                throw BadRequest($"Question {number} duplicates another question in this game.");

            result.Add(new LiveGameQuestionItem
            {
                Source = source,
                TeacherQuestionId = source == Mine ? item.TeacherQuestionId : null,
                ThinkingSkill = NormalizeThinkingSkill(item.ThinkingSkill),
                Question = normalized
            });
        }

        return result;
    }

    /// <summary>
    /// Game payload order. Only unchanged library questions keep their shared ID; others go to the game
    /// without one, so the game server records their answers as untracked instead of failing the match report.
    /// </summary>
    public static List<QuestionDataRequest> ForGame(IEnumerable<LiveGameQuestionItem> items)
        => items.Select(item =>
        {
            if (item.Source == Library) return item.Question;
            var copy = Clone(item.Question);
            copy.QuestionId = Guid.Empty;
            return copy;
        }).ToList();

    public static string? NormalizeThinkingSkill(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string? match = ThinkingSkills.FirstOrDefault(s => string.Equals(s, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw BadRequest("Thinking skill must be one of: " + string.Join(", ", ThinkingSkills) + ".");
    }

    public static string Serialize(List<LiveGameQuestionItem> items) => JsonSerializer.Serialize(items, JsonOptions);

    public static List<LiveGameQuestionItem> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<LiveGameQuestionItem>();
        try
        {
            return JsonSerializer.Deserialize<List<LiveGameQuestionItem>>(json, JsonOptions) ?? new List<LiveGameQuestionItem>();
        }
        catch (JsonException)
        {
            return new List<LiveGameQuestionItem>();
        }
    }

    public static QuestionDataRequest Clone(QuestionDataRequest question)
        => JsonSerializer.Deserialize<QuestionDataRequest>(JsonSerializer.Serialize(question, JsonOptions), JsonOptions)!;

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return Library;
        string value = source.Trim().ToLowerInvariant();
        return value is Library or Edited or Mine ? value : null;
    }

    private static string Json(QuestionDataRequest question) => JsonSerializer.Serialize(question, JsonOptions);

    public static GenericException BadRequest(string message) => new(ErrorCode.Failure, message, HttpStatusCode.BadRequest);
}
