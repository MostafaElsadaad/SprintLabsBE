using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.ResetMissions;
public class ResetMissionsCommand : IRequest<MissionResetResponse>
{
    public long UserId { get; set; }
}
