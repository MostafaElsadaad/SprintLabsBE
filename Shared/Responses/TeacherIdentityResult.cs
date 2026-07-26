namespace Shared.Responses;

public class TeacherIdentityResult
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
