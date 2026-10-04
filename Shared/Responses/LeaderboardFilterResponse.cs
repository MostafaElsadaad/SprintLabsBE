namespace Shared.Responses;

public class LeaderboardFilterResponse
{
    public long GradeId { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
}
