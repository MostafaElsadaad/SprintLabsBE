using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.SeedMissions;
public class SeedMissionsCommand : IRequest<List<long>>
{
    public long UserId { get; set; }
}
