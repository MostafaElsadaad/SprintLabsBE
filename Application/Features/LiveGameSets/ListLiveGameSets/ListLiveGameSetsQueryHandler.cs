using System.Net;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGameSets.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.ListLiveGameSets;

public class ListLiveGameSetsQueryHandler(IUserService users, IBaseRepository<LiveGameQuestionSet> repository)
    : IRequestHandler<ListLiveGameSetsQuery, List<LiveGameQuestionSetResponse>>
{
    public async Task<List<LiveGameQuestionSetResponse>> Handle(ListLiveGameSetsQuery request, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, request.AuthenticatedUserId);
        var query = repository.AsQueryable().AsNoTracking().Where(s => s.OwnerUserId == request.AuthenticatedUserId);
        if (request.LiveGameSetId.HasValue)
        {
            var set = await query.FirstOrDefaultAsync(s => s.Id == request.LiveGameSetId, cancellationToken)
                ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
            return new List<LiveGameQuestionSetResponse> { LiveGameSetMapper.Map(set, includeQuestions: true) };
        }

        var sets = await query.OrderByDescending(s => s.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        return sets.Select(s => LiveGameSetMapper.Map(s, includeQuestions: false)).ToList();
    }
}
