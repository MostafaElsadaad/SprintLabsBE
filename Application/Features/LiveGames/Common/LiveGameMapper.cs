using System.Text.Json;
using Domain.Models;
using Domain.Services;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.Common;

internal static class LiveGameMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static LiveGameResponse Map(LiveGame game, ILiveGameStateStore state)
    {
        var response = new LiveGameResponse
        {
            Id = game.Id,
            Code = game.Code,
            Title = game.Title,
            Status = game.Status.ToString(),
            Curriculum = game.Curriculum,
            Grade = game.Grade,
            Subject = game.Subject,
            Unit = game.Unit,
            Lesson = game.Lesson,
            Term = game.Term,
            Language = game.Language,
            MaxPlayers = game.MaxPlayers,
            BotCount = game.BotCount,
            CreatedAt = game.CreatedAt,
            StartedAt = game.StartedAt,
            EndedAt = game.EndedAt
        };
        state.Fill(game.Id, response);

        // After an API restart the in-memory view is empty; fall back to the stored final standings.
        if (response.Players.Count == 0 && !string.IsNullOrWhiteSpace(game.ResultJson))
        {
            try
            {
                response.Players = JsonSerializer.Deserialize<List<LiveGamePlayerResponse>>(game.ResultJson, JsonOptions)
                    ?? new List<LiveGamePlayerResponse>();
            }
            catch (JsonException)
            {
                response.Players = new List<LiveGamePlayerResponse>();
            }
        }

        return response;
    }

    public static string SerializeStandings(List<LiveGamePlayerResponse> standings)
        => JsonSerializer.Serialize(standings, JsonOptions);
}
