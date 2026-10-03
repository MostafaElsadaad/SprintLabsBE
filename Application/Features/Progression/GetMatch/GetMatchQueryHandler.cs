using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetMatch;

public class GetMatchQueryHandler : IRequestHandler<GetMatchQuery, MatchDetailResponse>
{
    private readonly IProgressionReadService _service;
    public GetMatchQueryHandler(IProgressionReadService service) => _service = service;
    public Task<MatchDetailResponse> Handle(GetMatchQuery request, CancellationToken cancellationToken)
        => _service.GetMatchAsync(request.UserId, request.MatchId, cancellationToken);
}
