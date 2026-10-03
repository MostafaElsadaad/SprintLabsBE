namespace Application.Features.CommunityDashboard.Common;

public class StudentView
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? PlayerProfileId { get; set; }
    public long ClassId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public string ActivityStatus { get; set; } = "UNKNOWN";
    public decimal? AvgScore { get; set; }
    public int? SessionsCount { get; set; }
}
