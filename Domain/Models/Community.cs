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
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<StudentLicense> StudentLicenses { get; set; } = new List<StudentLicense>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<MatchRewardResult> MatchRewardResults { get; set; } = new List<MatchRewardResult>();
    public ICollection<PlayerXpLog> PlayerXpLogs { get; set; } = new List<PlayerXpLog>();
    public ICollection<PlayerRankLog> PlayerRankLogs { get; set; } = new List<PlayerRankLog>();
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

public class Grade
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    // Null retains legacy/custom rows safely until an operator remediates them.
    public int? Value { get; set; }
    public string Name { get; set; } = default!;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Community Community { get; set; } = default!;
    public ICollection<Class> Classes { get; set; } = new List<Class>();
}

public class Class
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public string Name { get; set; } = default!;
    public ClassStatus Status { get; set; } = ClassStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Community Community { get; set; } = default!;
    public Grade Grade { get; set; } = default!;
    public ICollection<TeacherClassAssignment> TeacherClassAssignments { get; set; } = new List<TeacherClassAssignment>();
}
