namespace Application.Features.Communities.Students.Common;

public class CommunityStudentAnalyticsResponse
{
    public int CompletedAssignments { get; set; }
    public decimal? AverageScore { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int MatchesPlayed { get; set; }
    public decimal? WinRate { get; set; }
}
