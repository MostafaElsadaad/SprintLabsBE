using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.CreateMissionTemplate;
public class CreateMissionTemplateCommand : IRequest<long>
{
    public long UserId { get; set; }
    public MissionTemplateRequest Request { get; set; } = new();
}
