using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.ListLiveGames;

public class ListLiveGamesQueryHandler(IUserService users, IBaseRepository<LiveGame> liveGames, ILiveGameStateStore state)
    : IRequestHandler<ListLiveGamesQuery, List<LiveGameResponse>>
{
    public async Task<List<LiveGameResponse>> Handle(ListLiveGamesQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var games = await liveGames.AsQueryable().AsNoTracking()
            .OrderByDescending(g => g.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        return games.Select(g => LiveGameMapper.Map(g, state)).ToList();
    }
}
