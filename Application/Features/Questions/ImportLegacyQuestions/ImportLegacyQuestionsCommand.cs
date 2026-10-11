using MediatR;
using Shared.Requests.QuestionData;

namespace Application.Features.Questions.ImportLegacyQuestions;

public class ImportLegacyQuestionsCommand : IRequest<List<QuestionDataRequest>>
{
    public long UserId { get; set; }
    public long PackId { get; set; }
}
