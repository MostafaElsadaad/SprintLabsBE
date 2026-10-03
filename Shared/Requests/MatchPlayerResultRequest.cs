namespace Shared.Requests;

public class MatchPlayerResultRequest
{
    public long PlayerProfileId { get; set; }
    public int Position { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int MaxStreak { get; set; }
    public long AnswerTimeTotalMs { get; set; }
    public long QuestionTimeTotalMs { get; set; }
}
