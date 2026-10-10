namespace Application.Features.Communities.Students.Common;

public class StudentPerformance
{
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public long CorrectAnswers { get; set; }
    public long WrongAnswers { get; set; }
    public decimal AverageScore { get; set; }
    public decimal WinRate { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
