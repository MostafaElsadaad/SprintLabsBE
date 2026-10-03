namespace Shared.Responses;

public class MatchRewardResponse
{
    public long PlayerProfileId { get; set; }
    public int AnswerXp { get; set; }
    public int MatchResultXp { get; set; }
    public int MissionXp { get; set; }
    public int XpGained { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldTotalXp { get; set; }
    public int NewTotalXp { get; set; }
    public int RpChange { get; set; }
    public int OldRp { get; set; }
    public int NewRp { get; set; }
    public string OldRankTier { get; set; } = string.Empty;
    public string NewRankTier { get; set; } = string.Empty;
}
