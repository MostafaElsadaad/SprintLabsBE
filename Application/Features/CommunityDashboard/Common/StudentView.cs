namespace Application.Features.CommunityDashboard.Common;

public class StudentView
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long? PlayerProfileId { get; set; }
    public long ClassId { get; set; }
    public string StudentCode => $"STU-{Id:D6}";
    public string ClassName { get; set; } = string.Empty;
    public GradeSummary Grade { get; set; } = new();
    public DateTime JoinedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string Status => LicenseStatus.ToUpperInvariant() == "REVOKED" ? "INACTIVE" : LicenseStatus.ToUpperInvariant();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public string ActivityStatus { get; set; } = "UNKNOWN";
    public decimal? AvgScore { get; set; }
    public int? SessionsCount { get; set; }
}
