using System.Text.Json;
using Shared.Requests.LiveGames;

namespace Application.Features.LiveGames.Common;

/// <summary>
/// Cleans and stores the subjects/units/lessons a live game or saved set covers. The single
/// Subject/Unit/Lesson columns stay filled when exactly one is chosen, so older readers keep working.
/// </summary>
public static class LiveGameScopeRules
{
    public const int MaxSubjects = 20;
    public const int MaxUnits = 50;
    public const int MaxLessons = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds the scope from the list fields, falling back to the single values when a list is null.
    /// Lessons whose unit is not among the chosen units are dropped. Throws 400 for invalid values.
    /// </summary>
    public static LiveGameScope Normalize(List<string>? subjects, List<int>? units, List<LiveGameLessonRef>? lessons,
        string? subject, int? unit, int? lesson)
    {
        var subjectList = subjects ?? (string.IsNullOrWhiteSpace(subject) ? new List<string>() : new List<string> { subject });
        var unitList = units ?? (unit is > 0 ? new List<int> { unit.Value } : new List<int>());
        var lessonList = lessons
            ?? (unit is > 0 && lesson is > 0 ? new List<LiveGameLessonRef> { new() { Unit = unit.Value, Lesson = lesson.Value } } : new());

        var scope = new LiveGameScope
        {
            Subjects = subjectList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
            Units = unitList.Distinct().OrderBy(u => u).ToList()
        };
        if (scope.Subjects.Any(s => s.Length > 64) || scope.Units.Any(u => u < 1)
            || lessonList.Any(l => l == null || l.Unit < 1 || l.Lesson < 1))
            throw LiveGameQuestionNormalizer.BadRequest("Units and lessons must be positive and subjects 64 characters or fewer.");
        if (scope.Subjects.Count > MaxSubjects || scope.Units.Count > MaxUnits || lessonList.Count > MaxLessons)
            throw LiveGameQuestionNormalizer.BadRequest(
                $"Choose at most {MaxSubjects} subjects, {MaxUnits} units and {MaxLessons} lessons.");

        scope.Lessons = lessonList
            .Where(l => scope.Units.Count == 0 || scope.Units.Contains(l.Unit))
            .GroupBy(l => (l.Unit, l.Lesson)).Select(g => new LiveGameLessonRef { Unit = g.Key.Unit, Lesson = g.Key.Lesson })
            .OrderBy(l => l.Unit).ThenBy(l => l.Lesson).ToList();
        return scope;
    }

    public static string? SingleSubject(LiveGameScope scope) => scope.Subjects.Count == 1 ? scope.Subjects[0] : null;

    public static int? SingleUnit(LiveGameScope scope) => scope.Units.Count == 1 ? scope.Units[0] : null;

    public static int? SingleLesson(LiveGameScope scope)
        => scope.Units.Count == 1 && scope.Lessons.Count == 1 ? scope.Lessons[0].Lesson : null;

    /// <summary>JSON to store, or null when the single columns already describe the scope fully.</summary>
    public static string? Serialize(LiveGameScope scope)
        => scope.Subjects.Count <= 1 && scope.Units.Count <= 1 && scope.Lessons.Count <= 1
            ? null
            : JsonSerializer.Serialize(scope, JsonOptions);

    /// <summary>Reads the stored scope; rows without ScopeJson use their single columns.</summary>
    public static LiveGameScope Read(string? scopeJson, string? subject, int? unit, int? lesson)
    {
        if (!string.IsNullOrWhiteSpace(scopeJson))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<LiveGameScope>(scopeJson, JsonOptions);
                if (stored != null)
                {
                    stored.Subjects ??= new();
                    stored.Units ??= new();
                    stored.Lessons ??= new();
                    return stored;
                }
            }
            catch (JsonException)
            {
                // Fall back to the single columns below.
            }
        }

        return new LiveGameScope
        {
            Subjects = string.IsNullOrWhiteSpace(subject) ? new() : new() { subject },
            Units = unit is > 0 ? new() { unit.Value } : new(),
            Lessons = unit is > 0 && lesson is > 0 ? new() { new LiveGameLessonRef { Unit = unit.Value, Lesson = lesson.Value } } : new()
        };
    }
}
