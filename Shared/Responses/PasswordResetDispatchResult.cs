namespace Shared.Responses;

public class PasswordResetDispatchResult
{
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? ResetToken { get; set; }
}