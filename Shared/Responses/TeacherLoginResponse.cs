namespace Shared.Responses;

public class TeacherLoginResponse : TeacherTokenResponse
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Teacher";
    public TeacherCommunityResponse Community { get; set; } = new();
}