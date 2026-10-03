namespace Shared.Responses;

public class MatchPlayerResponse
{
    public long PlayerProfileId { get; set; }
    public int Position { get; set; }
    public bool IsWinner { get; set; }
    public int? CorrectAnswers { get; set; }
    public int? WrongAnswers { get; set; }
    public int? MaxStreak { get; set; }
    public long? AnswerTimeTotalMs { get; set; }
    public long? QuestionTimeTotalMs { get; set; }
    public MatchRewardResponse? Reward { get; set; }
}
