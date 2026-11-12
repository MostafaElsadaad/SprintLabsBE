using System.Net;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGames.Common;
using Application.Features.TeacherQuestions.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.QuestionData;
using Shared.Responses.LiveGames;

namespace Application.Features.TeacherQuestions.SaveTeacherQuestion;

public class SaveTeacherQuestionCommandHandler(
    IUserService users,
    IBaseRepository<TeacherQuestion> repository,
    IQuestionDataService questions)
    : IRequestHandler<SaveTeacherQuestionCommand, TeacherQuestionResponse>
{
    public async Task<TeacherQuestionResponse> Handle(SaveTeacherQuestionCommand command, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, command.AuthenticatedUserId);
        if (command.Request?.Question == null) throw LiveGameQuestionNormalizer.BadRequest(ErrorMessage.InvalidInput);

        TeacherQuestion? entity = null;
        if (command.TeacherQuestionId.HasValue)
        {
            entity = await repository.AsQueryable()
                .FirstOrDefaultAsync(q => q.Id == command.TeacherQuestionId && q.OwnerUserId == command.AuthenticatedUserId, cancellationToken)
                ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        }

        var question = LiveGameQuestionNormalizer.Clone(command.Request.Question);
        question.QuestionId = entity?.QuestionId ?? Guid.NewGuid();
        QuestionDataRequest normalized;
        try
        {
            normalized = questions.NormalizeQuestion(question);
        }
        catch (GenericException exception)
        {
            throw LiveGameQuestionNormalizer.BadRequest(exception.Message);
        }
        string? skill = LiveGameQuestionNormalizer.NormalizeThinkingSkill(command.Request.ThinkingSkill);

        if (entity == null)
        {
            entity = new TeacherQuestion { OwnerUserId = command.AuthenticatedUserId, CreatedAt = DateTime.UtcNow };
            TeacherQuestionMapper.Apply(entity, normalized, skill);
            await repository.AddAsync(entity);
        }
        else
        {
            TeacherQuestionMapper.Apply(entity, normalized, skill);
        }

        await repository.SaveChangesAsync();
        return TeacherQuestionMapper.Map(entity);
    }
}
