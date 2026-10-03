using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetMatchHistory;

public class GetMatchHistoryQueryHandler : IRequestHandler<GetMatchHistoryQuery, ProgressionPage<MatchHistoryResponse>>
{
    private readonly IProgressionReadService _service;
    public GetMatchHistoryQueryHandler(IProgressionReadService service) => _service = service;
    public Task<ProgressionPage<MatchHistoryResponse>> Handle(GetMatchHistoryQuery request, CancellationToken cancellationToken)
        => _service.GetHistoryAsync(request.UserId, request.CommunityId, request.PlayerProfileId, request.Page, cancellationToken);
}
