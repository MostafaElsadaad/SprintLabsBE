using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Requests.QuestionData;

namespace Infrastructure.Services;

/// <summary>Question helpers for live games: validation, lookups and game-format projection.</summary>
public partial class QuestionDataService
{
    public QuestionDataRequest NormalizeQuestion(QuestionDataRequest request)
        => QuestionDataMapping.Question(QuestionDataValidation.Question(request));

    public JsonElement ProjectQuestions(IReadOnlyList<QuestionDataRequest> questions)
        => LegacyQuestionConverter.ProjectBank(questions);

    public async Task<List<Shared.Responses.LiveGames.LessonOptionResponse>> GetLessonsAsync(
        string curriculum, int grade, IReadOnlyCollection<string> subjects, IReadOnlyCollection<int> units, CancellationToken ct)
    {
        var unitList = units.Distinct().ToList();
        var subjectList = subjects.Distinct().ToList();
        var query = context.Set<Domain.Models.QuestionData.Question>().AsNoTracking()
            .Where(x => x.Curriculum == curriculum && x.Grade == grade && unitList.Contains(x.Unit));
        if (subjectList.Count > 0) query = query.Where(x => subjectList.Contains(x.Subject));
        var rows = await query.GroupBy(x => new { x.Unit, x.Lesson })
            .Select(g => new { g.Key.Unit, g.Key.Lesson, Count = g.Count() })
            .ToListAsync(ct);
        return rows.OrderBy(x => x.Unit).ThenBy(x => x.Lesson)
            .Select(x => new Shared.Responses.LiveGames.LessonOptionResponse { Unit = x.Unit, Lesson = x.Lesson, QuestionCount = x.Count })
            .ToList();
    }

    public async Task<List<QuestionDataRequest>> GetLiveGamePoolAsync(
        string curriculum, int grade, Shared.Requests.LiveGames.LiveGameScope scope, int maxQuestions, CancellationToken ct)
    {
        var subjects = scope.Subjects;
        var units = scope.Units;
        // A unit with chosen lessons uses only those lessons; a unit without any uses all of its lessons.
        var unitsWithLessons = scope.Lessons.Select(l => l.Unit).Distinct().ToList();
        var wholeUnits = units.Where(u => !unitsWithLessons.Contains(u)).ToList();
        var lessonKeys = scope.Lessons.Select(l => l.Unit * 1000 + l.Lesson).ToList();

        var query = Questions().AsNoTracking().Where(x => x.Curriculum == curriculum && x.Grade == grade);
        if (subjects.Count > 0) query = query.Where(x => subjects.Contains(x.Subject));
        if (units.Count > 0 || lessonKeys.Count > 0)
            query = query.Where(x => wholeUnits.Contains(x.Unit) || lessonKeys.Contains(x.Unit * 1000 + x.Lesson));
        var rows = await query.OrderBy(x => x.QuestionId).Take(maxQuestions).ToListAsync(ct);
        return rows.Select(QuestionDataMapping.Question).ToList();
    }

    public async Task<List<QuestionDataRequest>> GetQuestionsByIdAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken ct)
    {
        if (questionIds == null || questionIds.Count == 0) return new List<QuestionDataRequest>();
        var ids = questionIds.Distinct().ToList();
        var rows = await Questions().AsNoTracking().Where(x => ids.Contains(x.QuestionId)).ToListAsync(ct);
        return rows.Select(QuestionDataMapping.Question).ToList();
    }
}
