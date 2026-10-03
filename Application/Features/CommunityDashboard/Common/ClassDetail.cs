namespace Application.Features.CommunityDashboard.Common;

public class ClassDetail : ClassView
{
    public bool IsOwner { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public decimal? AvgScore { get; set; }
    public int? InactiveStudentsCount { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool GameplayMetricsAvailable { get; set; }
    public List<StudentView> StudentsPreview { get; set; } = new();
}
