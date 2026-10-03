namespace Shared.Requests;
public class MissionActivationItemRequest
{
    public long MissionTemplateId { get; set; }
    public string AssignmentMode { get; set; } = "Fixed";
    public int Weight { get; set; } = 1;
    public int SortOrder { get; set; }
}
