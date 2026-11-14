using System.Security.Cryptography;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Domain.Services;
using MediatR;
using Shared.Requests.LiveGames;
using Shared.Requests.QuestionData;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.AutoSelectQuestions;

/// <summary>
/// Picks library questions for the chosen subjects/units/lessons, balanced across question types (round-robin over shuffled
/// type groups). Never generates questions. Also used to replace one question (Count = 1 with exclusions).
/// </summary>
public class AutoSelectQuestionsQueryHandler(IUserService users, IQuestionDataService questions)
    : IRequestHandler<AutoSelectQuestionsQuery, AutoSelectQuestionsResponse>
{
    private const int PoolLimit = 500;

    public async Task<AutoSelectQuestionsResponse> Handle(AutoSelectQuestionsQuery query, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, query.AuthenticatedUserId);
        var request = query.Request ?? new AutoSelectQuestionsRequest();
        string curriculum = (request.Curriculum ?? "").Trim();
        if (curriculum.Length == 0) throw LiveGameQuestionNormalizer.BadRequest("Choose a curriculum.");
        if (request.Grade is < 1 or > 12) throw LiveGameQuestionNormalizer.BadRequest("Choose a grade from 1 to 12.");
        if (request.Unit is <= 0 || request.Lesson is <= 0) throw LiveGameQuestionNormalizer.BadRequest("Unit and lesson must be positive.");
        var scope = LiveGameScopeRules.Normalize(request.Subjects, request.Units, request.Lessons, request.Subject, request.Unit, request.Lesson);
        if (request.Count < 1 || request.Count > LiveGameQuestionNormalizer.MaxQuestions)
            throw LiveGameQuestionNormalizer.BadRequest("Choose 1 to " + LiveGameQuestionNormalizer.MaxQuestions + " questions.");

        var excluded = new HashSet<Guid>(request.ExcludeQuestionIds ?? new List<Guid>());
        // Library questions with more answers than the game can show are never picked.
        var pool = (await questions.GetLiveGamePoolAsync(curriculum, request.Grade, scope, PoolLimit, cancellationToken))
            .Where(q => !excluded.Contains(q.QuestionId) && LiveGameAnswerLimits.Fits(q))
            .ToList();

        var groups = Shuffle(pool.GroupBy(q => q.QuestionType).Select(g => Shuffle(g.ToList())).ToList());
        var picked = new List<QuestionDataRequest>();
        while (picked.Count < request.Count && groups.Any(g => g.Count > 0))
        {
            foreach (var group in groups.Where(g => g.Count > 0))
            {
                if (picked.Count >= request.Count) break;
                picked.Add(group[0]);
                group.RemoveAt(0);
            }
        }

        return new AutoSelectQuestionsResponse
        {
            Questions = picked
                .Select(q => new LiveGameQuestionItem { Source = LiveGameQuestionNormalizer.Library, Question = q })
                .ToList(),
            AvailableCount = pool.Count,
            Warning = Warning(pool.Count, request.Count)
        };
    }

    private static string? Warning(int available, int requested)
    {
        if (available >= requested) return null;
        if (available == 0) return "No library questions match this selection. Add your own questions or choose other lessons.";
        string noun = available == 1 ? "question matches" : "questions match";
        return "Only " + available + " library " + noun + " this selection. Add your own questions or widen the selection.";
    }

    private static List<T> Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
        return items;
    }
}
