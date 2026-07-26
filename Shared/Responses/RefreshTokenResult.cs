namespace Shared.Responses;

public class RefreshTokenResult
{
    public long UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
}
