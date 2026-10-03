using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.SeedMissions;
public class SeedMissionsCommandHandler : IRequestHandler<SeedMissionsCommand, List<long>>
{
    private readonly IMissionService _service;
    public SeedMissionsCommandHandler(IMissionService service) => _service = service;
    public Task<List<long>> Handle(SeedMissionsCommand request, CancellationToken cancellationToken) => _service.SeedAsync(request.UserId, cancellationToken);
}
