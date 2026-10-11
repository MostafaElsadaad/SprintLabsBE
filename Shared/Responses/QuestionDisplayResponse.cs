namespace Shared.Responses;

public class QuestionDisplayResponse
{
    public Guid QuestionId { get; set; }
    public string Curriculum { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Term { get; set; }
    public int Unit { get; set; }
    public int Lesson { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public decimal TimerSeconds { get; set; }
    public List<QuestionDisplayOption> Choices { get; set; } = new();
    public List<QuestionDisplayOption> Items { get; set; } = new();
    public List<QuestionDisplayOption> MatchingLeft { get; set; } = new();
    public List<QuestionDisplayOption> MatchingRight { get; set; } = new();
    public List<QuestionDisplayOption> Options { get; set; } = new();
    public int BlankCount { get; set; }
}
