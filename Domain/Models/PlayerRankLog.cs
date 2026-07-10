using Domain.Enums;

namespace Domain.Models;

public class PlayerRankLog
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public long MatchId { get; set; }
    public int OldRp { get; set; }
    public int RpChange { get; set; }
    public int NewRp { get; set; }
    public RankTier OldRankTier { get; set; }
    public RankTier NewRankTier { get; set; }
    public int Position { get; set; }
    public int TotalPlayers { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Player PlayerProfile { get; set; } = default!;
    public Community? Community { get; set; }
    public Match Match { get; set; } = default!;
}
