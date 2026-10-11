using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Questions.GetQuestionHistory;

public class GetQuestionHistoryQueryHandler(IQuestionDataService service) : IRequestHandler<GetQuestionHistoryQuery, ProgressionPage<QuestionHistoryResponse>>
{
    public Task<ProgressionPage<QuestionHistoryResponse>> Handle(GetQuestionHistoryQuery request, CancellationToken cancellationToken)
        => service.HistoryAsync(request.UserId, request.PlayerId, request.MatchId, request.PageNumber, request.PageSize, cancellationToken);
}
