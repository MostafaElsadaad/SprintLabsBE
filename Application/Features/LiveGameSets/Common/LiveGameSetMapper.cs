using Application.Features.LiveGames.Common;
using Domain.Models;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.Common;

internal static class LiveGameSetMapper
{
    public static LiveGameQuestionSetResponse Map(LiveGameQuestionSet set, bool includeQuestions)
    {
        var scope = LiveGameScopeRules.Read(set.ScopeJson, set.Subject, set.Unit, set.Lesson);
        return new LiveGameQuestionSetResponse
        {
            Id = set.Id,
            Name = set.Name,
            Curriculum = set.Curriculum,
            Grade = set.Grade,
            Subject = set.Subject,
            Unit = set.Unit,
            Lesson = set.Lesson,
            QuestionCount = set.QuestionCount,
            CreatedAt = set.CreatedAt,
            UpdatedAt = set.UpdatedAt,
            Subjects = scope.Subjects,
            Units = scope.Units,
            Lessons = scope.Lessons,
            Questions = includeQuestions ? LiveGameQuestionNormalizer.Deserialize(set.QuestionsJson) : new()
        };
    }
}
