namespace Shared.Responses;

public class RpRankCalculationResult
{
    public int OldRp { get; set; }
    public int RpChange { get; set; }
    public int NewRp { get; set; }
    public int OldRankTier { get; set; }
    public int NewRankTier { get; set; }
    public int Position { get; set; }
    public int TotalPlayers { get; set; }
    public int MatchType { get; set; }
    public bool IsRanked { get; set; }
    public bool IsImmortal { get; set; }
}
