using MediatR;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.GetQuestionHistory;

public class GetQuestionHistoryQuery : IRequest<ProgressionPage<QuestionHistoryResponse>>
{
    public long? UserId { get; set; }
    public long PlayerId { get; set; }
    public long? MatchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
