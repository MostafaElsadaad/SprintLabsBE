namespace Shared.Responses;

public class PlayerRankingResponse
{
    public long PlayerProfileId { get; set; }
    public int Rp { get; set; }
    public string RankTier { get; set; } = string.Empty;
    public string HighestRankTier { get; set; } = string.Empty;
}
