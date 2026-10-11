using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Questions.RecordQuestionHistory;

public class RecordQuestionHistoryCommandHandler(IQuestionDataService service) : IRequestHandler<RecordQuestionHistoryCommand, QuestionHistoryResponse>
{
    public Task<QuestionHistoryResponse> Handle(RecordQuestionHistoryCommand request, CancellationToken cancellationToken)
        => service.RecordAsync(request.Request, cancellationToken, request.HistoryId);
}
