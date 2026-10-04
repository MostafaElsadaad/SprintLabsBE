namespace Shared.Requests;

public class LeaderboardRequest : ProgressionPageRequest
{
    public string Period { get; set; } = "AllTime";
}
