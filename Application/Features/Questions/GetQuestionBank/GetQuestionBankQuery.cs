using MediatR;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.GetQuestionBank;

public class GetQuestionBankQuery : IRequest<ProgressionPage<QuestionDataRequest>>
{
    public long UserId { get; set; }
    public bool IsGameServer { get; set; }
    public QuestionBankFilter Filter { get; set; } = new();
}
