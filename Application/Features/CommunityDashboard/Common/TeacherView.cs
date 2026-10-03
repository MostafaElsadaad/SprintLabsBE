namespace Application.Features.CommunityDashboard.Common;

public class TeacherView
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TeacherCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public List<GradeSummary> Grades { get; set; } = new();
    public List<ClassView> Classes { get; set; } = new();
    public int? StudentsCount { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastObservedActivityAt { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public List<ActivityView> RecentActivity { get; set; } = new();
}
