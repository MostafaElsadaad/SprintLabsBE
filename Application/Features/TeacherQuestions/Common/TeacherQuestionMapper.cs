using System.Text.Json;
using Domain.Models;
using Shared.Requests.QuestionData;
using Shared.Responses.LiveGames;

namespace Application.Features.TeacherQuestions.Common;

internal static class TeacherQuestionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static TeacherQuestionResponse Map(TeacherQuestion entity) => new()
    {
        Id = entity.Id,
        ThinkingSkill = entity.ThinkingSkill,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Question = JsonSerializer.Deserialize<QuestionDataRequest>(entity.PayloadJson, JsonOptions) ?? new QuestionDataRequest()
    };

    public static void Apply(TeacherQuestion entity, QuestionDataRequest question, string? thinkingSkill)
    {
        entity.QuestionId = question.QuestionId;
        entity.Curriculum = question.Curriculum;
        entity.Grade = question.Grade;
        entity.Subject = question.Subject;
        entity.Unit = question.Unit;
        entity.Lesson = question.Lesson;
        entity.QuestionType = question.QuestionType;
        entity.QuestionText = question.QuestionText;
        entity.ThinkingSkill = thinkingSkill;
        entity.PayloadJson = JsonSerializer.Serialize(question, JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
