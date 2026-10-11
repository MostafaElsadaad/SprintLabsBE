using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Questions.PublishQuestion;

public class PublishQuestionCommandHandler(IQuestionDataService service) : IRequestHandler<PublishQuestionCommand, Guid>
{
    public Task<Guid> Handle(PublishQuestionCommand request, CancellationToken cancellationToken)
        => service.PublishAsync(request.UserId, request.Request, cancellationToken);
}
