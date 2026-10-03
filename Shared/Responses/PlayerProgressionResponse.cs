namespace Shared.Responses;

public class PlayerProgressionResponse
{
    public long PlayerProfileId { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
    public int Gold { get; set; }
    public int Rp { get; set; }
    public string RankTier { get; set; } = string.Empty;
    public string HighestRankTier { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int XpIntoCurrentLevel { get; set; }
    public int NextLevelXpRequirement { get; set; }
}
