namespace Shared.Responses;

public class LeaderboardStandingResponse
{
    public string Period { get; set; } = "AllTime";
    public DateTime? PeriodStartsAt { get; set; }
    public DateTime? PeriodEndsAt { get; set; }
    public int Total { get; set; }
    public LeaderboardEntryResponse? CurrentPlayer { get; set; }
    public List<LeaderboardFilterResponse> AvailableFilters { get; set; } = new();
}
