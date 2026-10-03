using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetRanking;

public class GetRankingQueryHandler : IRequestHandler<GetRankingQuery, PlayerRankingResponse>
{
    private readonly IProgressionReadService _service;
    public GetRankingQueryHandler(IProgressionReadService service) => _service = service;
    public Task<PlayerRankingResponse> Handle(GetRankingQuery request, CancellationToken cancellationToken)
        => _service.GetRankingAsync(request.UserId, request.PlayerProfileId, cancellationToken);
}
