using Domain.Enums;
namespace Domain.Models;
public class PlayerMissionAssignment
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long MissionActivationId { get; set; }
    public DateTime AssignedAt { get; set; }
}
