namespace Domain.Models;

public class MatchPlayer
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public long PlayerProfileId { get; set; }
    public long? CommunityId { get; set; }
    public int Position { get; set; }
    public bool IsWinner { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int MaxStreak { get; set; }
    public long AnswerTimeTotalMs { get; set; }
    public long QuestionTimeTotalMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Match Match { get; set; } = default!;
    public Player PlayerProfile { get; set; } = default!;
    public Community? Community { get; set; }
}
