namespace Shared.Responses;

public class AccessTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}
