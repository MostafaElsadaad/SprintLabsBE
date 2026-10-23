using Domain.Services;
using MediatR;
using Shared.Responses;
namespace Application.Features.Missions.GetMissionHistory;
public class GetMissionHistoryQueryHandler : IRequestHandler<GetMissionHistoryQuery, List<PlayerMissionResponse>>
{
    private readonly IMissionService _service;
    public GetMissionHistoryQueryHandler(IMissionService service) => _service = service;
    public Task<List<PlayerMissionResponse>> Handle(GetMissionHistoryQuery request, CancellationToken ct)
        => _service.HistoryAsync(request.UserId, request.Page, request.PageSize, ct);
}
