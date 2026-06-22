using Domain.Models;

namespace Domain.Repositories;

public interface ICommunityRepository
{
    Task<bool> SlugExistsAsync(string slug);
    Task<Community> CreateCommunityAsync(Community community);
    Task<bool> CommunityExistsAsync(long communityId);
    Task<List<CommunityListItemProjection>> ListCommunitiesAsync();
    Task<CommunityOwnerProjection> UpsertOwnerAsync(long communityId, long userId);
    Task<CommunityLicense?> GetLicenseByCommunityIdAsync(long communityId);
    Task<CommunityLicense> UpsertLicenseAsync(
        long communityId,
        int maxStudents,
        int maxTeachers,
        int studentEmailChangeLimit);
}

public class CommunityListItemProjection
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CommunityOwnerProjection? Owner { get; set; }
    public CommunityLicenseProjection? License { get; set; }
}

public class CommunityOwnerProjection
{
    public long CommunityUserId { get; set; }
    public long CommunityId { get; set; }
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CommunityLicenseProjection
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public int MaxStudents { get; set; }
    public int UsedStudents { get; set; }
    public int MaxTeachers { get; set; }
    public int UsedTeachers { get; set; }
    public int StudentEmailChangeLimit { get; set; }
}
