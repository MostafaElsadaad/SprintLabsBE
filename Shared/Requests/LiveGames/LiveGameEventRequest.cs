namespace Shared.Requests.LiveGames;

/// <summary>
/// A server-derived fact reported by the dedicated game server. Types: player_joined, player_left,
/// match_started, start_failed, question_opened, answer_graded, score_changed, player_disconnected,
/// player_reconnected, player_result, match_ended.
/// </summary>
public class LiveGameEventRequest
{
    public string EventId { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public long? PlayerProfileId { get; set; }
    public string? DisplayName { get; set; }
    public int? QuestionNumber { get; set; }
    public string? QuestionType { get; set; }
    public string? QuestionText { get; set; }
    public bool? IsCorrect { get; set; }
    public int? Score { get; set; }
    public int? Rank { get; set; }
}
