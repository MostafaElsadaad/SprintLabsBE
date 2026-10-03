using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.CreateMissionTemplate;
public class CreateMissionTemplateCommandHandler : IRequestHandler<CreateMissionTemplateCommand, long>
{
    private readonly IMissionService _service;
    public CreateMissionTemplateCommandHandler(IMissionService service) => _service = service;
    public Task<long> Handle(CreateMissionTemplateCommand request, CancellationToken cancellationToken) => _service.CreateTemplateAsync(request.UserId, request.Request, cancellationToken);
}
