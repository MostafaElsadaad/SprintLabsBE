using System.Net;
using Application.Features.Admin.Communities.Common;
using Application.Features.LiveGameSets.Common;
using Application.Features.LiveGames.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGameSets.SaveLiveGameSet;

public class SaveLiveGameSetCommandHandler(
    IUserService users,
    IBaseRepository<LiveGameQuestionSet> repository,
    IQuestionDataService questions)
    : IRequestHandler<SaveLiveGameSetCommand, LiveGameQuestionSetResponse>
{
    public async Task<LiveGameQuestionSetResponse> Handle(SaveLiveGameSetCommand command, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, command.AuthenticatedUserId);
        var request = command.Request ?? throw LiveGameQuestionNormalizer.BadRequest(ErrorMessage.InvalidInput);
        string name = (request.Name ?? "").Trim();
        string curriculum = (request.Curriculum ?? "").Trim();
        if (name.Length is 0 or > 100) throw LiveGameQuestionNormalizer.BadRequest("Give the set a name of up to 100 characters.");
        if (curriculum.Length is 0 or > 64) throw LiveGameQuestionNormalizer.BadRequest("Choose a curriculum.");
        if (request.Grade is < 1 or > 12) throw LiveGameQuestionNormalizer.BadRequest("Choose a grade from 1 to 12.");
        string? subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim();
        if (subject?.Length > 64) throw LiveGameQuestionNormalizer.BadRequest(ErrorMessage.InvalidInput);

        var items = await LiveGameQuestionNormalizer.NormalizeAsync(questions, request.Questions, cancellationToken);

        LiveGameQuestionSet? set = null;
        if (command.LiveGameSetId.HasValue)
        {
            set = await repository.AsQueryable()
                .FirstOrDefaultAsync(s => s.Id == command.LiveGameSetId && s.OwnerUserId == command.AuthenticatedUserId, cancellationToken)
                ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
        }

        bool isNew = set == null;
        set ??= new LiveGameQuestionSet { OwnerUserId = command.AuthenticatedUserId, CreatedAt = DateTime.UtcNow };
        set.Name = name;
        set.Curriculum = curriculum;
        set.Grade = request.Grade;
        set.Subject = subject;
        set.Unit = request.Unit is > 0 ? request.Unit : null;
        set.Lesson = request.Lesson is > 0 ? request.Lesson : null;
        set.QuestionCount = items.Count;
        set.QuestionsJson = LiveGameQuestionNormalizer.Serialize(items);
        set.UpdatedAt = DateTime.UtcNow;
        if (isNew) await repository.AddAsync(set);
        await repository.SaveChangesAsync();
        return LiveGameSetMapper.Map(set, includeQuestions: true);
    }
}
