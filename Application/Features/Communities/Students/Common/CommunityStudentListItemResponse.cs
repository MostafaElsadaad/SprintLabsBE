namespace Application.Features.Communities.Students.Common;

public class CommunityStudentListItemResponse
{
    public long LicenseId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public long? PlayerProfileId { get; set; }
    public string? PlayerName { get; set; }
    public string? AvatarUrl { get; set; }
    public long GradeId { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
