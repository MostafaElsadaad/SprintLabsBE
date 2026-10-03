using MediatR;
using Domain.Services;
using Shared.Responses;
namespace Application.Features.Missions.PreviewMissionTemplates;
public class PreviewMissionTemplatesQueryHandler : IRequestHandler<PreviewMissionTemplatesQuery, List<MissionTemplateResponse>>
{
    private readonly IMissionService _service;
    public PreviewMissionTemplatesQueryHandler(IMissionService service) => _service = service;
    public Task<List<MissionTemplateResponse>> Handle(PreviewMissionTemplatesQuery request, CancellationToken cancellationToken) => _service.PreviewTemplatesAsync(request.UserId, cancellationToken);
}
