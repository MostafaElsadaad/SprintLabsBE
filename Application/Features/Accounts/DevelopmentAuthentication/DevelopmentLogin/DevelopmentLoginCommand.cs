using MediatR;

using Shared.Responses;

namespace Application.Features.Accounts.DevelopmentAuthentication.DevelopmentLogin;

public sealed class DevelopmentLoginCommand : IRequest<LoginResponse>
{
    public string AccountKey { get; set; } = string.Empty;
    public string[] SuppliedApiKeys { get; set; } = Array.Empty<string>();
}
