using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.CreateMissionActivation;
public class CreateMissionActivationCommandHandler : IRequestHandler<CreateMissionActivationCommand, long>
{
    private readonly IMissionService _service;
    public CreateMissionActivationCommandHandler(IMissionService service) => _service = service;
    public Task<long> Handle(CreateMissionActivationCommand request, CancellationToken cancellationToken) => _service.CreateActivationAsync(request.UserId, request.Request, cancellationToken);
}
