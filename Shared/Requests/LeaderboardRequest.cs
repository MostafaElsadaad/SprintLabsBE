namespace Shared.Requests;

public class LeaderboardRequest : ProgressionPageRequest
{
    public string SortBy { get; set; } = "Rank";
    public string Period { get; set; } = "AllTime";
}
