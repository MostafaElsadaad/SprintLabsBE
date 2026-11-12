using System.Net;
using System.Security.Cryptography;
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
using Shared.Requests.QuestionData;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.CreateLiveGame;

public class CreateLiveGameCommandHandler(
    IUserService users,
    IBaseRepository<LiveGame> liveGames,
    IQuestionDataService questions,
    ILiveGameStateStore state)
    : IRequestHandler<CreateLiveGameCommand, LiveGameResponse>
{
    public async Task<LiveGameResponse> Handle(CreateLiveGameCommand command, CancellationToken cancellationToken)
    {
        await AdminCommunityAuthorization.EnsurePlatformAdmin(users, command.AuthenticatedUserId);

        var request = command.Request;
        string title = (request.Title ?? "").Trim();
        if (title.Length == 0) title = "Live game";
        string curriculum = (request.Curriculum ?? "").Trim();
        string? subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim();
        string? language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        int maxPlayers = request.MaxPlayers == 0 ? LiveGameRules.DefaultMaxPlayers : request.MaxPlayers;

        if (title.Length > 100) throw BadRequest("The game name must be 100 characters or fewer.");
        if (curriculum.Length is 0 or > 64) throw BadRequest("Choose a curriculum.");
        if (request.Grade is < 1 or > 12) throw BadRequest("Choose a grade from 1 to 12.");
        if (subject?.Length > 64 || language?.Length > 16) throw BadRequest(ErrorMessage.InvalidInput);
        if (request.Unit is < 0 || request.Lesson is < 0 || request.Term is < 0) throw BadRequest(ErrorMessage.InvalidInput);
        if (maxPlayers < 1 || maxPlayers > LiveGameRules.MaxPlayersLimit)
            throw BadRequest($"Players per game must be 1 to {LiveGameRules.MaxPlayersLimit}.");

        var filter = new QuestionBankFilter
        {
            Grade = request.Grade,
            Curriculum = curriculum,
            Subject = subject,
            Unit = request.Unit,
            Lesson = request.Lesson,
            Term = request.Term,
            Language = language,
            Count = 1
        };
        string? questionsJson = null;
        if (request.Questions is { Count: > 0 })
        {
            var items = await LiveGameQuestionNormalizer.NormalizeAsync(questions, request.Questions, cancellationToken);
            questionsJson = LiveGameQuestionNormalizer.Serialize(items);
        }
        else if (await questions.ProjectBankAsync(filter, cancellationToken) == null)
        {
            throw BadRequest("No questions match this curriculum and grade. Change the filters and try again.");
        }

        var game = new LiveGame
        {
            Code = await CreateUniqueCodeAsync(cancellationToken),
            Title = title,
            Status = LiveGameStatus.Lobby,
            Curriculum = curriculum,
            Grade = request.Grade,
            Subject = subject,
            Unit = request.Unit,
            Lesson = request.Lesson,
            Term = request.Term,
            Language = language,
            MaxPlayers = maxPlayers,
            QuestionsJson = questionsJson,
            CreatedByUserId = command.AuthenticatedUserId,
            CreatedAt = DateTime.UtcNow
        };
        await liveGames.AddAsync(game);
        await liveGames.SaveChangesAsync();
        return LiveGameMapper.Map(game, state);
    }

    private async Task<string> CreateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            var chars = new char[LiveGameRules.CodeLength];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = LiveGameRules.CodeAlphabet[RandomNumberGenerator.GetInt32(LiveGameRules.CodeAlphabet.Length)];
            string code = new(chars);
            bool taken = await liveGames.AsQueryable().AnyAsync(
                g => g.Code == code && (g.Status == LiveGameStatus.Lobby || g.Status == LiveGameStatus.Running),
                cancellationToken);
            if (!taken) return code;
        }

        throw new GenericException(ErrorCode.Failure, "Could not create a game code. Try again.", HttpStatusCode.Conflict);
    }

    private static GenericException BadRequest(string message)
        => new(ErrorCode.Failure, message, HttpStatusCode.BadRequest);
}
