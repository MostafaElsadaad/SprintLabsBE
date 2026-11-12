using System.Net;
using Application.Features.Admin.Communities.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.LiveGameSets.DeleteLiveGameSet;

/// <summary>Games already launched from a set keep their own copy of its questions.</summary>
public class DeleteLiveGameSetCommandHandler(IUserService users, IBaseRepository<LiveGameQuestionSet> repository)
    : IRequestHandler<DeleteLiveGameSetCommand, bool>
{
    public async Task<bool> Handle(DeleteLiveGameSetCommand command, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, command.AuthenticatedUserId);
        var set = await repository.AsQueryable()
            .FirstOrDefaultAsync(s => s.Id == command.LiveGameSetId && s.OwnerUserId == command.AuthenticatedUserId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        await repository.DeleteAsync(set);
        await repository.SaveChangesAsync();
        return true;
    }
}
