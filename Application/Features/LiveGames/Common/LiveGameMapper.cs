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
        response.Questions = LiveGameQuestionNormalizer.Deserialize(game.QuestionsJson);
        response.QuestionCount = response.Questions.Count;
        state.Fill(game.Id, response);

        // After an API restart the in-memory view is empty; fall back to the stored results.
        if ((response.Players.Count == 0 || response.Rounds.Count == 0) && !string.IsNullOrWhiteSpace(game.ResultJson))
        {
            StoredResults stored = ReadResults(game.ResultJson);
            if (response.Players.Count == 0) response.Players = stored.Players;
            if (response.Rounds.Count == 0) response.Rounds = stored.Rounds;
        }

        return response;
    }

    /// <summary>Final (or latest) standings plus the round history, stored in LiveGame.ResultJson.</summary>
    public static string SerializeResults(List<LiveGamePlayerResponse> standings, List<LiveGameQuestionResponse> rounds)
        => JsonSerializer.Serialize(new StoredResults { Players = standings, Rounds = rounds }, JsonOptions);

    /// <summary>Reads ResultJson: an object with players and rounds, or the older plain standings array.</summary>
    public static StoredResults ReadResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new StoredResults();
        try
        {
            if (json.TrimStart().StartsWith('['))
            {
                return new StoredResults
                {
                    Players = JsonSerializer.Deserialize<List<LiveGamePlayerResponse>>(json, JsonOptions) ?? new()
                };
            }

            var stored = JsonSerializer.Deserialize<StoredResults>(json, JsonOptions) ?? new StoredResults();
            stored.Players ??= new List<LiveGamePlayerResponse>();
            stored.Rounds ??= new List<LiveGameQuestionResponse>();
            return stored;
        }
        catch (JsonException)
        {
            return new StoredResults();
        }
    }

    public sealed class StoredResults
    {
        public List<LiveGamePlayerResponse> Players { get; set; } = new();
        public List<LiveGameQuestionResponse> Rounds { get; set; } = new();
    }
}
