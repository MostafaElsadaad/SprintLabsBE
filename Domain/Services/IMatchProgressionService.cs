using Shared.Requests;
using Shared.Responses;

namespace Domain.Services;

public interface IMatchProgressionService
{
    Task<MatchRegistrationResponse> RegisterAsync(RegisterMatchRequest request, CancellationToken ct);
    Task<MatchCompletionResponse> CompleteAsync(long matchId, CompleteMatchRequest request, CancellationToken ct);
}
