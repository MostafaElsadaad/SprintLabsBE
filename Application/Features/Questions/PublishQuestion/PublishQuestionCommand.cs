using MediatR;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.PublishQuestion;

public class PublishQuestionCommand : IRequest<Guid>
{
    public long UserId { get; set; }
    public QuestionDataRequest Request { get; set; } = new();
}
