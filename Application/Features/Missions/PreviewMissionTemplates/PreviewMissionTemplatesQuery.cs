using MediatR;
using Shared.Requests;
using Shared.Responses;
namespace Application.Features.Missions.PreviewMissionTemplates;
public class PreviewMissionTemplatesQuery : IRequest<List<MissionTemplateResponse>>
{
    public long UserId { get; set; }
}
