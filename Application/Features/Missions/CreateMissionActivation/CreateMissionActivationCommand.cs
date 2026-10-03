using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.CreateMissionActivation;
public class CreateMissionActivationCommand : IRequest<long>
{
    public long UserId { get; set; }
    public MissionActivationRequest Request { get; set; } = new();
}
