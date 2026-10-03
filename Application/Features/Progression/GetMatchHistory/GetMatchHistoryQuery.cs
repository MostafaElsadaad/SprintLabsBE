using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetMatchHistory;

public class GetMatchHistoryQuery : IRequest<ProgressionPage<MatchHistoryResponse>>
{
    public long UserId { get; set; }
    public long? CommunityId { get; set; }
    public long? PlayerProfileId { get; set; }
    public ProgressionPageRequest Page { get; set; } = new();
}
