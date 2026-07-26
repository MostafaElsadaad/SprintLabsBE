namespace Shared.Responses;

public class TeacherInvitationIssueResult
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long CommunityId { get; set; }
    public string CommunityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InvitationToken { get; set; }
}
