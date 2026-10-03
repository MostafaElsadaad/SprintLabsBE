using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.GetMyMissions;
public class GetMyMissionsQuery : IRequest<List<PlayerMissionResponse>>
{
    public long UserId { get; set; }
}
