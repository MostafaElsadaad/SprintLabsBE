using Shared.Requests.QuestionData;
using Shared.Responses;
using Domain.Models;
using System.Text.Json;

namespace Domain.Services;

public interface IQuestionDataService
{
    Task<Guid> PublishAsync(long adminUserId, QuestionDataRequest request, CancellationToken ct);
    Task<ProgressionPage<QuestionDataRequest>> GetBankAsync(QuestionBankFilter filter, CancellationToken ct);
    Task<List<QuestionDataRequest>> ImportLegacyAsync(long adminUserId, long packId, CancellationToken ct);
    Task<JsonElement?> ProjectLegacyAsync(QuestionsJson pack, CancellationToken ct);
    Task<JsonElement?> ProjectBankAsync(QuestionBankFilter filter, CancellationToken ct);
    Task<QuestionHistoryResponse> RecordAsync(QuestionHistoryRequest request, CancellationToken ct, long? historyId = null);
    Task<ProgressionPage<QuestionHistoryResponse>> HistoryAsync(long? userId, long playerId, long? matchId,
        int pageNumber, int pageSize, CancellationToken ct);
}
