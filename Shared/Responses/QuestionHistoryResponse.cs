using Shared.Requests.QuestionData;

namespace Shared.Responses;

public class QuestionHistoryResponse : QuestionHistoryRequest
{
    public long HistoryId { get; set; }
    public bool IsCorrect { get; set; }
}
