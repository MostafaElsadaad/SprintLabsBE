using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Features.LiveGames.Common;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.QuestionData;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetLiveGameQuestions;

/// <summary>
/// Projects the game's question filter into the game's legacy pack format, shuffled, so each live
/// game plays a different set from the same curriculum and grade.
/// </summary>
public class GetLiveGameQuestionsQueryHandler(IBaseRepository<LiveGame> liveGames, IQuestionDataService questions)
    : IRequestHandler<GetLiveGameQuestionsQuery, LiveGameQuestionsResponse>
{
    public async Task<LiveGameQuestionsResponse> Handle(GetLiveGameQuestionsQuery request, CancellationToken cancellationToken)
    {
        string code = (request.Code ?? "").Trim().ToUpperInvariant();
        var game = await liveGames.AsQueryable().AsNoTracking()
            .Where(g => g.Code == code)
            .OrderByDescending(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        var payload = await questions.ProjectBankAsync(new QuestionBankFilter
        {
            Grade = game.Grade,
            Curriculum = game.Curriculum,
            Subject = game.Subject,
            Unit = game.Unit,
            Lesson = game.Lesson,
            Term = game.Term,
            Language = game.Language,
            Count = LiveGameRules.QuestionPoolSize
        }, cancellationToken) ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        return new LiveGameQuestionsResponse { PayloadJson = Shuffle(payload) };
    }

    private static JsonElement Shuffle(JsonElement payload)
    {
        if (JsonNode.Parse(payload.GetRawText()) is not JsonObject root) return payload;
        var property = root.FirstOrDefault(p => string.Equals(p.Key, "Questions", StringComparison.OrdinalIgnoreCase));
        if (property.Value is not JsonArray array) return payload;

        var items = array.Select(x => x?.DeepClone()).ToList();
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        root[property.Key] = new JsonArray(items.Take(LiveGameRules.QuestionsSentToServer).ToArray());
        return JsonSerializer.SerializeToElement(root);
    }
}
