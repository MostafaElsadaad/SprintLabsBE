namespace Shared.Requests;

public class MatchAnswerRequest
{
    public long PlayerProfileId { get; set; }
    public long? QuestionId { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int AnswerTimeMs { get; set; }
    public int QuestionTimeMs { get; set; }
    public int StreakBeforeAnswer { get; set; }
    public int StreakAfterAnswer { get; set; }
}
