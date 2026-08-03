namespace Shared.Responses;

public class TeacherInvitationValidationResult
{
    public string CommunityName { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}