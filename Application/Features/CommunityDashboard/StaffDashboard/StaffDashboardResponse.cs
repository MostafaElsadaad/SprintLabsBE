using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.StaffDashboard;

public class StaffDashboardResponse
{
    public string Role { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int NewStudentsThisWeek { get; set; }
    public int ActiveClasses { get; set; }
    public int GradesCount { get; set; }
    public int? TeachersCount { get; set; }
    public int? PendingInvitationsCount { get; set; }
    public bool GameplayMetricsAvailable { get; set; }
    public int? SessionsThisWeek { get; set; }
    public decimal? SessionsChangeVsLastWeekPercent { get; set; }
    public decimal? GameplayHours { get; set; }
    public decimal? GameplayHoursThisWeek { get; set; }
    public decimal? AvgHoursPerStudent { get; set; }
    public decimal? GameplayHoursChangeVsPreviousMonthPercent { get; set; }
    public List<ClassView> Classes { get; set; } = new();
}
