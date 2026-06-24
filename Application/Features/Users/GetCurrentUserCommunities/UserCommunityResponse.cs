namespace Application.Features.Users.GetCurrentUserCommunities;

public class UserCommunityResponse
{
    public long CommunityId { get; set; }
    public string CommunityName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CommunityStatus { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
}
