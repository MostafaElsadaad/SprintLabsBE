namespace Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

public class CommunityLoginRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
