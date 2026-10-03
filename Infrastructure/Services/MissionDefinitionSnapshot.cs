using Shared.Requests;
namespace Infrastructure.Services;
internal class MissionDefinitionSnapshot
{
    public MissionTemplateRequest Rules { get; set; } = new();
    public List<MissionBoxSnapshot> Boxes { get; set; } = new();
}
