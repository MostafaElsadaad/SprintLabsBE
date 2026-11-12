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
        string curriculum, int grade, string? subject, int unit, CancellationToken ct)
    {
        var query = context.Set<Domain.Models.QuestionData.Question>().AsNoTracking()
            .Where(x => x.Curriculum == curriculum && x.Grade == grade && x.Unit == unit);
        if (subject != null) query = query.Where(x => x.Subject == subject);
        var rows = await query.GroupBy(x => x.Lesson)
            .Select(g => new { Lesson = g.Key, Count = g.Count() })
            .OrderBy(x => x.Lesson)
            .ToListAsync(ct);
        return rows.Select(x => new Shared.Responses.LiveGames.LessonOptionResponse { Lesson = x.Lesson, QuestionCount = x.Count }).ToList();
    }

    public async Task<List<QuestionDataRequest>> GetQuestionsByIdAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken ct)
    {
        if (questionIds == null || questionIds.Count == 0) return new List<QuestionDataRequest>();
        var ids = questionIds.Distinct().ToList();
        var rows = await Questions().AsNoTracking().Where(x => ids.Contains(x.QuestionId)).ToListAsync(ct);
        return rows.Select(QuestionDataMapping.Question).ToList();
    }
}
