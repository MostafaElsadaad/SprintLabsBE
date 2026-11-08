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

namespace Application.Features.LiveGames.StartLiveGame;

public class StartLiveGameCommandHandler(
    IUserService users,
    IBaseRepository<LiveGame> liveGames,
    ILiveGameStateStore state,
    ILiveGameBroadcaster broadcaster)
    : IRequestHandler<StartLiveGameCommand, LiveGameResponse>
{
    public async Task<LiveGameResponse> Handle(StartLiveGameCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var game = await liveGames.AsQueryable()
            .FirstOrDefaultAsync(g => g.Id == request.LiveGameId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        if (game.Status == LiveGameStatus.Running) return LiveGameMapper.Map(game, state);
        if (game.Status != LiveGameStatus.Lobby)
            throw new GenericException(ErrorCode.Failure, "This game has already ended.", HttpStatusCode.BadRequest);
        if (state.JoinedCount(game.Id) < 1)
            throw new GenericException(ErrorCode.Failure, "Nobody has joined yet. Wait for at least one player.", HttpStatusCode.BadRequest);

        game.Status = LiveGameStatus.Running;
        game.StartedAt = DateTime.UtcNow;
        await liveGames.SaveChangesAsync();

        var snapshot = LiveGameMapper.Map(game, state);
        await broadcaster.PublishAsync(snapshot, cancellationToken);
        return snapshot;
    }
}
