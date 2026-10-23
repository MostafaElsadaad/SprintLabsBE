using MediatR;
namespace Application.Features.Missions.SeedGameMissions;
public class SeedGameMissionsCommand : IRequest<List<long>>
{
    public long UserId { get; set; }
}
