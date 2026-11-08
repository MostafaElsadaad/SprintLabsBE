using Application.Features.LiveGames.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetActiveLiveGames;

public class GetActiveLiveGamesQueryHandler(IBaseRepository<LiveGame> liveGames, ILiveGameStateStore state)
    : IRequestHandler<GetActiveLiveGamesQuery, List<ActiveLiveGameResponse>>
{
    public async Task<List<ActiveLiveGameResponse>> Handle(GetActiveLiveGamesQuery request, CancellationToken cancellationToken)
    {
        state.MarkGameServerSeen();
        DateTime endedAfter = DateTime.UtcNow - LiveGameRules.EndedGameServerWindow;
        var games = await liveGames.AsQueryable().AsNoTracking()
            .Where(g => g.Status == LiveGameStatus.Lobby || g.Status == LiveGameStatus.Running ||
                        (g.EndedAt != null && g.EndedAt >= endedAfter))
            .OrderBy(g => g.Id)
            .Take(100)
            .ToListAsync(cancellationToken);
        return games.Select(g => new ActiveLiveGameResponse
        {
            Code = g.Code,
            Status = g.Status.ToString(),
            MaxPlayers = g.MaxPlayers
        }).ToList();
    }
}
