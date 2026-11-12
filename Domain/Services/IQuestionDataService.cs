using Shared.Requests.QuestionData;
using Shared.Responses;
using Domain.Models;
using System.Text.Json;

namespace Domain.Services;

public interface IQuestionDataService
{
    Task<List<JourneyOptionResponse>> GetJourneyCatalogueAsync(CancellationToken ct);
    Task<Guid> PublishAsync(long adminUserId, QuestionDataRequest request, CancellationToken ct);
    Task<ProgressionPage<QuestionDataRequest>> GetBankAsync(QuestionBankFilter filter, CancellationToken ct);
    Task<List<QuestionDataRequest>> ImportLegacyAsync(long adminUserId, long packId, CancellationToken ct);
    Task<JsonElement?> ProjectLegacyAsync(QuestionsJson pack, CancellationToken ct);
    Task<JsonElement?> ProjectBankAsync(QuestionBankFilter filter, CancellationToken ct);
    /// <summary>Validates a question (correct answer, ids, positions) and returns its canonical form; throws 400.</summary>
    QuestionDataRequest NormalizeQuestion(QuestionDataRequest request);
    /// <summary>Projects an ordered question list into the game pack format.</summary>
    JsonElement ProjectQuestions(IReadOnlyList<QuestionDataRequest> questions);
    Task<List<QuestionDataRequest>> GetQuestionsByIdAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken ct);
    Task<List<Shared.Responses.LiveGames.LessonOptionResponse>> GetLessonsAsync(string curriculum, int grade, string? subject, int unit, CancellationToken ct);
    Task<QuestionHistoryResponse> RecordAsync(QuestionHistoryRequest request, CancellationToken ct, long? historyId = null);
    Task<ProgressionPage<QuestionHistoryResponse>> HistoryAsync(long? userId, long playerId, long? matchId,
        int pageNumber, int pageSize, CancellationToken ct);
}
