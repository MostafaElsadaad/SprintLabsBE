using Domain.Services;
using MediatR;
using Shared.Requests.QuestionData;

namespace Application.Features.Questions.ImportLegacyQuestions;

public class ImportLegacyQuestionsCommandHandler(IQuestionDataService service)
    : IRequestHandler<ImportLegacyQuestionsCommand, List<QuestionDataRequest>>
{
    public Task<List<QuestionDataRequest>> Handle(ImportLegacyQuestionsCommand request, CancellationToken cancellationToken)
        => service.ImportLegacyAsync(request.UserId, request.PackId, cancellationToken);
}
