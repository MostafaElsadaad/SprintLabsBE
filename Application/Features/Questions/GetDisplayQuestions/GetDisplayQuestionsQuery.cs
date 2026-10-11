using MediatR;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.GetDisplayQuestions;

public class GetDisplayQuestionsQuery : IRequest<ProgressionPage<QuestionDisplayResponse>>
{
    public long UserId { get; set; }
    public QuestionBankFilter Filter { get; set; } = new();
}
