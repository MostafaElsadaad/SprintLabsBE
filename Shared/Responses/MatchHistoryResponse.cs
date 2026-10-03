namespace Shared.Responses;

public class MatchHistoryResponse
{
    public long MatchId { get; set; }
    public string MatchCode { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? CommunityId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalPlayers { get; set; }
    public int? Position { get; set; }
    public MatchRewardResponse? Reward { get; set; }
}
