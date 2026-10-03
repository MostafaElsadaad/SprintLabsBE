using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.CompleteMatch;

public class CompleteMatchCommandHandler : IRequestHandler<CompleteMatchCommand, MatchCompletionResponse>
{
    private readonly IMatchProgressionService _service;
    public CompleteMatchCommandHandler(IMatchProgressionService service) => _service = service;
    public Task<MatchCompletionResponse> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
        => _service.CompleteAsync(request.MatchId, request.Request, cancellationToken);
}
