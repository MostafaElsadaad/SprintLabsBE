using Domain.Enums;
namespace Domain.Models;
public class MissionActivationItem
{
    public long Id { get; set; }
    public long MissionActivationId { get; set; }
    public long MissionTemplateId { get; set; }
    public MissionAssignmentMode AssignmentMode { get; set; }
    public int Weight { get; set; } = 1;
    public int SortOrder { get; set; }
}
