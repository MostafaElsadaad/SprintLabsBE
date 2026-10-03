using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.ProcessMissionEvents;
public class ProcessMissionEventsCommandHandler : IRequestHandler<ProcessMissionEventsCommand, List<MissionEventResponse>>
{
    private readonly IMissionService _service;
    public ProcessMissionEventsCommandHandler(IMissionService service) => _service = service;
    public Task<List<MissionEventResponse>> Handle(ProcessMissionEventsCommand request, CancellationToken cancellationToken) => _service.EventsAsync(request.Events, cancellationToken);
}
