using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetMatch;

public class GetMatchQuery : IRequest<MatchDetailResponse>
{
    public long UserId { get; set; }
    public long MatchId { get; set; }
}
