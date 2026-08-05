using Shared.Enums;

namespace Shared.Responses;

public class RefreshTokenResult
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AuthenticatedAccountType AccountType { get; set; } = AuthenticatedAccountType.Teacher;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}
