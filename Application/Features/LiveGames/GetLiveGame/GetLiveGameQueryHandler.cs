using System.Net;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetLiveGame;

public class GetLiveGameQueryHandler(IUserService users, IBaseRepository<LiveGame> liveGames, ILiveGameStateStore state)
    : IRequestHandler<GetLiveGameQuery, LiveGameResponse>
{
    public async Task<LiveGameResponse> Handle(GetLiveGameQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var game = await liveGames.AsQueryable().AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.LiveGameId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        return LiveGameMapper.Map(game, state);
    }
}
