namespace Domain.Models;

/// <summary>A saved, ordered question selection that can launch new live games.</summary>
public class LiveGameQuestionSet
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public int QuestionCount { get; set; }
    public string QuestionsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
