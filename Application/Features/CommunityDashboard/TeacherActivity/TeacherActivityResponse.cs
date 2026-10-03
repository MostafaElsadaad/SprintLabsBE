namespace Application.Features.CommunityDashboard.TeacherActivity;

public class TeacherActivityResponse
{
    public long TeacherId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int ClassesCount { get; set; }
    public int? TotalSessions { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public int ObservedEventsCount { get; set; }
    public DateTime? LastObservedActivityAt { get; set; }
}
