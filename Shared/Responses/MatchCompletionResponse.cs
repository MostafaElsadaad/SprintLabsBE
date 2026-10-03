namespace Shared.Responses;

public class MatchCompletionResponse
{
    public long MatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<MatchRewardResponse> Rewards { get; set; } = new();
}
