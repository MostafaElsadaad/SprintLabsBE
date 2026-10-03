using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.ResetMissions;
public class ResetMissionsCommandHandler : IRequestHandler<ResetMissionsCommand, MissionResetResponse>
{
    private readonly IMissionService _service;
    public ResetMissionsCommandHandler(IMissionService service) => _service = service;
    public Task<MissionResetResponse> Handle(ResetMissionsCommand request, CancellationToken cancellationToken) => _service.ResetAsync(request.UserId, cancellationToken);
}
