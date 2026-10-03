using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.ProcessMissionEvents;
public class ProcessMissionEventsCommand : IRequest<List<MissionEventResponse>>
{
    public List<MissionEventRequest> Events { get; set; } = new();
}
