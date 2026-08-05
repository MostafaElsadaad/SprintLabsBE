namespace Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;

public class RegisterCommunityRequest
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
