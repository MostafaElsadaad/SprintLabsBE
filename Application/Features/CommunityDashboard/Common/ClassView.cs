namespace Application.Features.CommunityDashboard.Common;

public class ClassView
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GradeSummary Grade { get; set; } = new();
    public List<TeacherSummary> Teachers { get; set; } = new();
    public int StudentsCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
