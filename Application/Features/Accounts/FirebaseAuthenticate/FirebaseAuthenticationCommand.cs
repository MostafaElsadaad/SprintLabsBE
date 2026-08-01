using MediatR;

using Shared.Responses;

namespace Application.Features.Accounts.FirebaseAuthenticate;

public class FirebaseAuthenticationCommand : IRequest<LoginResponse>
{
    public string IdToken { get; set; } = string.Empty;
}
