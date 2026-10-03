namespace Shared.Responses;

public class LeaderboardEntryResponse
{
    public long Position { get; set; }
    public long PlayerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rp { get; set; }
    public string RankTier { get; set; } = string.Empty;
    public int Level { get; set; }
    public int TotalWins { get; set; }
}
