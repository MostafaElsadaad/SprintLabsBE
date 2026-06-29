namespace Application.Features.Communities.StudentLicenses.Common;

public class StudentLicenseResponse
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public string Email { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public long? PlayerProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public StudentLicenseGradeResponse Grade { get; set; } = new();
    public StudentLicenseClassResponse Class { get; set; } = new();
    public int EmailChangeCount { get; set; }
    public long AssignedByUserId { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
