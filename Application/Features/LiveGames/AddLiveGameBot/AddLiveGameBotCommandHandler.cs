using System.Net;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.AddLiveGameBot;

public class AddLiveGameBotCommandHandler(
    IUserService users,
    IBaseRepository<LiveGame> liveGames,
    ILiveGameStateStore state,
    ILiveGameBroadcaster broadcaster)
    : IRequestHandler<AddLiveGameBotCommand, LiveGameResponse>
{
    public async Task<LiveGameResponse> Handle(AddLiveGameBotCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var game = await liveGames.AsQueryable()
            .FirstOrDefaultAsync(g => g.Id == request.LiveGameId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        if (game.Status != LiveGameStatus.Lobby)
            throw new GenericException(ErrorCode.Failure, "Bots can only be added before the game starts.", HttpStatusCode.BadRequest);
        if (game.BotCount + state.JoinedCount(game.Id) >= game.MaxPlayers)
            throw new GenericException(ErrorCode.Failure, "The room is full.", HttpStatusCode.BadRequest);

        game.BotCount++;
        await liveGames.SaveChangesAsync();

        var snapshot = LiveGameMapper.Map(game, state);
        await broadcaster.PublishAsync(snapshot, cancellationToken);
        return snapshot;
    }
}
