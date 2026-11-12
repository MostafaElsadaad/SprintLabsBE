using System.Net;
using Application.Features.Admin.Communities.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.TeacherQuestions.DeleteTeacherQuestion;

/// <summary>Deleting a personal question does not change games or saved sets that already copied it.</summary>
public class DeleteTeacherQuestionCommandHandler(IUserService users, IBaseRepository<TeacherQuestion> repository)
    : IRequestHandler<DeleteTeacherQuestionCommand, bool>
{
    public async Task<bool> Handle(DeleteTeacherQuestionCommand command, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, command.AuthenticatedUserId);
        var entity = await repository.AsQueryable()
            .FirstOrDefaultAsync(q => q.Id == command.TeacherQuestionId && q.OwnerUserId == command.AuthenticatedUserId, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        await repository.DeleteAsync(entity);
        await repository.SaveChangesAsync();
        return true;
    }
}
