using MediatR;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.RecordQuestionHistory;

public class RecordQuestionHistoryCommand : IRequest<QuestionHistoryResponse>
{
    public QuestionHistoryRequest Request { get; set; } = new();
    public long? HistoryId { get; set; }
}
