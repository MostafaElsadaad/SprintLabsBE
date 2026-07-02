namespace Application.Features.Communities.Students.Common;

public class CommunityStudentDetailResponse
{
    public long UserId { get; set; }
    public long PlayerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Gold { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public CommunityStudentAnalyticsResponse Analytics { get; set; } = new();
}
