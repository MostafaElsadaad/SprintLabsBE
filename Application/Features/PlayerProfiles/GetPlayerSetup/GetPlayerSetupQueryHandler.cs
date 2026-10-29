using Application.Features.PlayerProfiles.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
namespace Application.Features.PlayerProfiles.GetPlayerSetup;

public class GetPlayerSetupQueryHandler(IUserService users, IPlayerRepository players,
    IBaseRepository<StudentLicense> licenses) : IRequestHandler<GetPlayerSetupQuery, PlayerSetupResponse>
{
    public async Task<PlayerSetupResponse> Handle(GetPlayerSetupQuery request, CancellationToken cancellationToken)
    {
        var player = await PlayerSetupPolicy.LoadAsync(request.UserId, users, players);
        return PlayerSetupPolicy.Map(player, await PlayerSetupPolicy.IsCommunityManagedAsync(player, licenses, cancellationToken));
    }
}
