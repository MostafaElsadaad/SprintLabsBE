namespace Shared.Responses;

public class TeacherIdentityResult
{
    public long UserId { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
