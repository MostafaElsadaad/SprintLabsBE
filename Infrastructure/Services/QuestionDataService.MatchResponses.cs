using Domain.Models.QuestionData;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;

namespace Infrastructure.Services;

public partial class QuestionDataService
{
    // The completion service owns the transaction, roster validation and replay lock.
    internal static async Task StageMatchResponsesAsync(ApplicationDbContext database, long matchId,
        IReadOnlyList<MatchAnswerRequest> answers, CancellationToken ct)
    {
        var submitted = answers.Where(x => x.QuestionResponse != null).ToList();
        if (submitted.Count == 0) return;
        var ids = submitted.Select(x => x.QuestionResponse!.QuestionId).Distinct().ToList();
        var questions = await Questions(database).AsNoTracking().Where(x => ids.Contains(x.QuestionId))
            .ToDictionaryAsync(x => x.QuestionId, ct);
        foreach (var answer in submitted)
        {
            var response = answer.QuestionResponse!;
            if (response.PlayerId != answer.PlayerProfileId || response.MatchId != matchId ||
                response.TimeTakenSeconds != answer.AnswerTimeMs / 1000m ||
                !questions.TryGetValue(response.QuestionId, out var question) ||
                !string.Equals(question.QuestionType.ToString(), answer.QuestionType, StringComparison.OrdinalIgnoreCase))
                throw Error("Question response must match its human player, match, type and elapsed time.");
            QuestionDataValidation.Seconds(response.TimeTakenSeconds, allowZero: true);
            var history = new QuestionHistory { PlayerId = response.PlayerId, MatchId = matchId,
                QuestionId = response.QuestionId, TimeTakenSeconds = response.TimeTakenSeconds };
            AddResponse(question, response, history);
            if (Correct(question, response) != answer.IsCorrect)
                throw Error("Question correctness does not match the published answer.");
            database.Set<QuestionHistory>().Add(history);
        }
    }
}
