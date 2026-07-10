namespace Domain.Models;

public class MatchQuestionResult
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public long PlayerProfileId { get; set; }
    public long? QuestionId { get; set; }
    public string QuestionType { get; set; } = default!;
    public bool IsCorrect { get; set; }
    public int AnswerTimeMs { get; set; }
    public int QuestionTimeMs { get; set; }
    public int StreakBeforeAnswer { get; set; }
    public int StreakAfterAnswer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Match Match { get; set; } = default!;
    public Player PlayerProfile { get; set; } = default!;
}
