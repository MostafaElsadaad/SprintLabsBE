using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetLeaderboard;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardPageResponse>
{
    private readonly IProgressionReadService _service;
    public GetLeaderboardQueryHandler(IProgressionReadService service) => _service = service;
    public Task<LeaderboardPageResponse> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
        => _service.GetLeaderboardAsync(request.UserId, request.CommunityId, request.Page, cancellationToken);
}
