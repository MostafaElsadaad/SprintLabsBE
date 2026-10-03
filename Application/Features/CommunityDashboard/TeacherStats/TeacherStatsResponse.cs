namespace Application.Features.CommunityDashboard.TeacherStats;

public class TeacherStatsResponse
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int PendingInvitations { get; set; }
}
