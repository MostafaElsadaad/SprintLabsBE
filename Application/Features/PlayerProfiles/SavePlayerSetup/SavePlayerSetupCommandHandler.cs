using System.Net;
using Application.Features.PlayerProfiles.Common;
using Application.Features.PlayerProfiles.GetSetupOptions;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
namespace Application.Features.PlayerProfiles.SavePlayerSetup;

public class SavePlayerSetupCommandHandler(IUserService users, IPlayerRepository players,
    IBaseRepository<StudentLicense> licenses, IMediator mediator) : IRequestHandler<SavePlayerSetupCommand, PlayerSetupResponse>
{
    public async Task<PlayerSetupResponse> Handle(SavePlayerSetupCommand request, CancellationToken cancellationToken)
    {
        var player = await PlayerSetupPolicy.LoadAsync(request.UserId, users, players);
        if (await PlayerSetupPolicy.IsCommunityManagedAsync(player, licenses, cancellationToken))
            throw PlayerSetupPolicy.Error(HttpStatusCode.Forbidden);
        var nickname = request.Nickname?.Trim();
        var curriculum = request.Curriculum?.Trim();
        var language = request.Language?.Trim();
        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 100 || nickname.Any(char.IsControl) ||
            request.Grade is < 7 or > 12 || string.IsNullOrWhiteSpace(curriculum) || curriculum.Length > 120 ||
            string.IsNullOrWhiteSpace(language) || language.Length > 40)
            throw PlayerSetupPolicy.Error(HttpStatusCode.BadRequest);
        var options = await mediator.Send(new GetSetupOptionsQuery(), cancellationToken);
        if (!options.EducationOptions.Any(x => x.Curriculum == curriculum && x.Language == language))
            throw PlayerSetupPolicy.Error(HttpStatusCode.BadRequest);
        // Validate the whole form before mutating the tracked player; wallet/account fields are never assigned.
        player.Name = nickname;
        player.Grade = request.Grade;
        player.Curriculum = curriculum;
        player.Language = language;
        await players.UpdatePlayer(player);
        return PlayerSetupPolicy.Map(player, false);
    }
}
