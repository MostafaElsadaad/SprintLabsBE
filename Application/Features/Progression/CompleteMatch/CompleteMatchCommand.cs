using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.CompleteMatch;

public class CompleteMatchCommand : IRequest<MatchCompletionResponse>
{
    public long MatchId { get; set; }
    public CompleteMatchRequest Request { get; set; } = new();
}
