using System.Text.Json;
namespace Shared.Requests;
public class MissionTemplateRequest
{
    public string MissionKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string PeriodType { get; set; } = "";
    public string EventType { get; set; } = "";
    public string ProgressType { get; set; } = "";
    public int TargetValue { get; set; }
    public Dictionary<string, JsonElement> Conditions { get; set; } = new();
    public string ProgressField { get; set; } = "value";
    public string? ResetEventType { get; set; }
    public bool MatchScoped { get; set; }
    public bool RepeatValue { get; set; }
    public List<MissionRewardRequest> Rewards { get; set; } = new();
}
