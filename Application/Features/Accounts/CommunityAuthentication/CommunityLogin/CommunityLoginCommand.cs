using MediatR;

namespace Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

public class CommunityLoginCommand : IRequest<CommunityLoginResponse>
{
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? CreatedByIp { get; set; }
}
