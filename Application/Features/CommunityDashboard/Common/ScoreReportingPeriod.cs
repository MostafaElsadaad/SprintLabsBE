namespace Application.Features.CommunityDashboard.Common;

public class ScoreReportingPeriod
{
    public string Type { get; init; } = "ALL_TIME";
    public string Name { get; init; } = "All recorded activity";
    public DateOnly? StartDate => null;
    public DateOnly? EndDate => null;
    public string Definition { get; init; } = "Correct answers / (correct + wrong answers) * 100 across all persisted completed matches for this player and community. No term or academic-year date filter; no answers yields zero.";

    public static ScoreReportingPeriod Unavailable => new()
    {
        Type = "UNAVAILABLE", Name = "Not available",
        Definition = "Class average-score aggregation is not available; avgScore is the existing zero placeholder and has no reporting period."
    };
}
