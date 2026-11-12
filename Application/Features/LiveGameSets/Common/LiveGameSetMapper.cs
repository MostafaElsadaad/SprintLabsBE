using Application.Features.LiveGames.Common;
using Domain.Models;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.Common;

internal static class LiveGameSetMapper
{
    public static LiveGameQuestionSetResponse Map(LiveGameQuestionSet set, bool includeQuestions) => new()
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
        Questions = includeQuestions ? LiveGameQuestionNormalizer.Deserialize(set.QuestionsJson) : new()
    };
}
