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

namespace Application.Features.LiveGames.EndLiveGame;

/// <summary>
/// Ends a game from the dashboard. A lobby is cancelled; a running game is completed and the game
/// server ends the match on its next poll, then reports the final standings.
/// </summary>
public class EndLiveGameCommandHandler(
    IUserService users,
    IBaseRepository<LiveGame> liveGames,
    ILiveGameStateStore state,
    ILiveGameBroadcaster broadcaster)
    : IRequestHandler<EndLiveGameCommand, LiveGameResponse>
{
    public async Task<LiveGameResponse> Handle(EndLiveGameCommand request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var game = await liveGames.AsQueryable()
            .FirstOrDefaultAsync(g => g.Id == request.LiveGameId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        if (game.Status is LiveGameStatus.Lobby or LiveGameStatus.Running)
        {
            game.Status = game.Status == LiveGameStatus.Lobby ? LiveGameStatus.Cancelled : LiveGameStatus.Completed;
            game.EndedAt = DateTime.UtcNow;
            var standings = state.Standings(game.Id);
            if (standings.Count > 0) game.ResultJson = LiveGameMapper.SerializeStandings(standings);
            await liveGames.SaveChangesAsync();
        }

        var snapshot = LiveGameMapper.Map(game, state);
        await broadcaster.PublishAsync(snapshot, cancellationToken);
        return snapshot;
    }
}
