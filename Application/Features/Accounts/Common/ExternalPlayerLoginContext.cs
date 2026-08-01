namespace Application.Features.Accounts.Common;

public class ExternalPlayerLoginContext
{
    public string Subject { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PictureUrl { get; set; } = string.Empty;
    public string? GoogleProviderId { get; set; }
    public bool ActivatePendingTeacherMemberships { get; set; }
}
