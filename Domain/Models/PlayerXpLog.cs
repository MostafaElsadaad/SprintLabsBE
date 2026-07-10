using Domain.Enums;

namespace Domain.Models;

public class PlayerXpLog
{
    public long Id { get; set; }
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public XpSourceType SourceType { get; set; }
    public long? SourceId { get; set; }
    public int BaseXp { get; set; }
    public decimal Multiplier { get; set; }
    public int FinalXp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Player PlayerProfile { get; set; } = default!;
    public Community? Community { get; set; }
}
