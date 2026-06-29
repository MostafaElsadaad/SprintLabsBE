using Domain.Enums;

namespace Domain.Models;

public class StudentLicense
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public string Email { get; set; } = default!;
    public long? UserId { get; set; }
    public long? PlayerProfileId { get; set; }
    public long GradeId { get; set; }
    public long ClassId { get; set; }
    public StudentLicenseStatus Status { get; set; } = StudentLicenseStatus.Pending;
    public int EmailChangeCount { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Community Community { get; set; } = default!;
    public Grade Grade { get; set; } = default!;
    public Class Class { get; set; } = default!;
}
