using System.Net;
using Application.Features.LiveGames.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.LiveGames.RecordLiveGameEvents;

public class RecordLiveGameEventsCommandHandler(
    IBaseRepository<LiveGame> liveGames,
    ILiveGameStateStore state,
    ILiveGameBroadcaster broadcaster)
    : IRequestHandler<RecordLiveGameEventsCommand, int>
{
    public async Task<int> Handle(RecordLiveGameEventsCommand request, CancellationToken cancellationToken)
    {
        if (request.Events == null || request.Events.Count > 200)
            throw new GenericException(ErrorCode.Failure, "Send 0 to 200 events per batch.", HttpStatusCode.BadRequest);
        state.MarkGameServerSeen();

        string code = (request.Code ?? "").Trim().ToUpperInvariant();
        var game = await liveGames.AsQueryable()
            .Where(g => g.Code == code)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        int applied = 0;
        bool ended = false;
        bool roundOpened = false;
        foreach (var liveEvent in request.Events.OrderBy(e => e.OccurredAt))
        {
            if (!state.Apply(game.Id, liveEvent)) continue;
            applied++;
            if (liveEvent.Type == "match_ended") ended = true;
            if (liveEvent.Type == "question_opened") roundOpened = true;
        }

        if (ended)
        {
            if (game.Status is LiveGameStatus.Lobby or LiveGameStatus.Running)
            {
                game.Status = LiveGameStatus.Completed;
                game.EndedAt = DateTime.UtcNow;
            }
            game.ResultJson = LiveGameMapper.SerializeResults(state.Standings(game.Id), state.Rounds(game.Id));
            await liveGames.SaveChangesAsync();
        }
        else if (roundOpened && game.Status == LiveGameStatus.Running)
        {
            // Save progress each round so the history survives an API restart in the middle of a game.
            game.ResultJson = LiveGameMapper.SerializeResults(state.Standings(game.Id), state.Rounds(game.Id));
            await liveGames.SaveChangesAsync();
        }

        if (applied > 0)
            await broadcaster.PublishAsync(LiveGameMapper.Map(game, state), cancellationToken);
        return applied;
    }
}
