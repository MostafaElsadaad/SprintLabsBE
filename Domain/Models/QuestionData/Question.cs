using Domain.Enums;

namespace Domain.Models.QuestionData;

public class Question
{
    public Guid QuestionId { get; set; }
    public string Curriculum { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Term { get; set; }
    public int Unit { get; set; }
    public int Lesson { get; set; }
    public QuestionType QuestionType { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public decimal TimerSeconds { get; set; } = 10m;
    public List<MCQChoice> Choices { get; set; } = new();
    public List<TrueFalseAnswer> BooleanAnswers { get; set; } = new();
    public List<FillBlankAnswer> AcceptedAnswers { get; set; } = new();
    public List<OrderingItem> Items { get; set; } = new();
    public List<MatchingPair> Pairs { get; set; } = new();
    public List<DragDropOption> Options { get; set; } = new();
    public List<DragDropAnswer> BlankAnswers { get; set; } = new();
}
