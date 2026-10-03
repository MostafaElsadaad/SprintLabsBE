using Shared.Requests;
namespace Shared.Responses;
public class MissionTemplateResponse
{
    public long MissionTemplateId { get; set; }
    public bool IsActive { get; set; }
    public MissionTemplateRequest Definition { get; set; } = new();
}
