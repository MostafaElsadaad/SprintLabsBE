using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetRanking;

public class GetRankingQuery : IRequest<PlayerRankingResponse>
{
    public long UserId { get; set; }
    public long? PlayerProfileId { get; set; }
}
