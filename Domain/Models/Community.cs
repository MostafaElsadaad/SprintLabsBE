using Domain.Enums;

namespace Domain.Models;

public class Community
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public CommunityStatus Status { get; set; } = CommunityStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<CommunityUser> CommunityUsers { get; set; } = new List<CommunityUser>();
    public CommunityLicense? License { get; set; }
}

public class CommunityUser
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public long UserId { get; set; }
    public CommunityUserRole Role { get; set; }
    public CommunityUserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Community Community { get; set; } = default!;
}

public class CommunityLicense
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public int MaxStudents { get; set; }
    public int UsedStudents { get; set; }
    public int MaxTeachers { get; set; }
    public int UsedTeachers { get; set; }
    public int StudentEmailChangeLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Community Community { get; set; } = default!;
}
