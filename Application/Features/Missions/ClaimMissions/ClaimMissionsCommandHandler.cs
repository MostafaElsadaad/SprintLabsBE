using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.ClaimMissions;
public class ClaimMissionsCommandHandler : IRequestHandler<ClaimMissionsCommand, List<MissionClaimResponse>>
{
    private readonly IMissionService _service;
    public ClaimMissionsCommandHandler(IMissionService service) => _service = service;
    public Task<List<MissionClaimResponse>> Handle(ClaimMissionsCommand request, CancellationToken cancellationToken) => _service.ClaimAsync(request.UserId, request.MissionId, cancellationToken);
}
