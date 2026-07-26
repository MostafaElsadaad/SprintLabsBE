namespace Shared.Responses;

public class TeacherInvitationAcceptanceResponse
{
    public long CommunityId { get; set; }
    public string CommunityName { get; set; } = string.Empty;
    public string Role { get; set; } = "Teacher";
    public string Status { get; set; } = "Active";
}
