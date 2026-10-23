using Domain.Services;
using MediatR;
namespace Application.Features.Missions.SeedGameMissions;
public class SeedGameMissionsCommandHandler : IRequestHandler<SeedGameMissionsCommand, List<long>>
{
    private readonly IMissionService _service;
    public SeedGameMissionsCommandHandler(IMissionService service) => _service = service;
    public Task<List<long>> Handle(SeedGameMissionsCommand request, CancellationToken ct)
        => _service.SeedGameCatalogueAsync(request.UserId, ct);
}
