using Shared.Requests;

namespace Shared.Responses;

public class MatchDetailResponse
{
    public MatchHistoryResponse Match { get; set; } = new();
    public List<MatchPlayerResponse> Players { get; set; } = new();
    public List<MatchAnswerRequest> QuestionResults { get; set; } = new();
}
