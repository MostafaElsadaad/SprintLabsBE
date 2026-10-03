using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.GetMyMissions;
public class GetMyMissionsQueryHandler : IRequestHandler<GetMyMissionsQuery, List<PlayerMissionResponse>>
{
    private readonly IMissionService _service;
    public GetMyMissionsQueryHandler(IMissionService service) => _service = service;
    public Task<List<PlayerMissionResponse>> Handle(GetMyMissionsQuery request, CancellationToken cancellationToken) => _service.GetMyAsync(request.UserId, cancellationToken);
}
