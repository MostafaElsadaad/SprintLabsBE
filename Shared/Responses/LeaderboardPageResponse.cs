namespace Shared.Responses;

public class LeaderboardPageResponse : ProgressionPage<LeaderboardEntryResponse>
{
    public string Period { get; set; } = "AllTime";
    public DateTime? PeriodStartsAt { get; set; }
    public DateTime? PeriodEndsAt { get; set; }
    public LeaderboardEntryResponse? CurrentPlayer { get; set; }
    public List<LeaderboardFilterResponse> AvailableFilters { get; set; } = new();
}
