using MediatR;

namespace Application.Features.Accounts.DevelopmentAuthentication.ListDevelopmentPlayers;

public sealed class ListDevelopmentPlayersQuery : IRequest<List<DevelopmentPlayerResponse>>
{
    public string[] SuppliedApiKeys { get; set; } = Array.Empty<string>();
}
