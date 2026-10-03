using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.ClaimMissions;
public class ClaimMissionsCommand : IRequest<List<MissionClaimResponse>>
{
    public long UserId { get; set; }
    public long? MissionId { get; set; }
}
