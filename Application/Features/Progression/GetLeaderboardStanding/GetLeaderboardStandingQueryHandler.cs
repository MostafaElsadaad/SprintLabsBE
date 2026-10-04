using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetLeaderboardStanding;

public class GetLeaderboardStandingQueryHandler : IRequestHandler<GetLeaderboardStandingQuery, LeaderboardStandingResponse>
{
    private readonly IProgressionReadService _service;
    public GetLeaderboardStandingQueryHandler(IProgressionReadService service) => _service = service;
    public Task<LeaderboardStandingResponse> Handle(GetLeaderboardStandingQuery request, CancellationToken cancellationToken)
        => _service.GetLeaderboardStandingAsync(request.UserId, request.CommunityId, request.Request, cancellationToken);
}
