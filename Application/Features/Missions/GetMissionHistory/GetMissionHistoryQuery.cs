using MediatR;
using Shared.Responses;
namespace Application.Features.Missions.GetMissionHistory;
public class GetMissionHistoryQuery : IRequest<List<PlayerMissionResponse>>
{
    public long UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
