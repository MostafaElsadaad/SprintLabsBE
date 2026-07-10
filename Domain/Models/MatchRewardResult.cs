using Domain.Enums;

namespace Domain.Models;

public class MatchRewardResult
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public int AnswerXp { get; set; }
    public int MatchResultXp { get; set; }
    public int MissionXp { get; set; }
    public int TotalXp { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldTotalXp { get; set; }
    public int NewTotalXp { get; set; }
    public int RpChange { get; set; }
    public int OldRp { get; set; }
    public int NewRp { get; set; }
    public RankTier OldRankTier { get; set; }
    public RankTier NewRankTier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Match Match { get; set; } = default!;
    public Player PlayerProfile { get; set; } = default!;
    public Community? Community { get; set; }
}
