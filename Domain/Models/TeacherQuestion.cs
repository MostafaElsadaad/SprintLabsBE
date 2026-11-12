namespace Domain.Models;

/// <summary>
/// A question written by a teacher or admin for their own games. Private to its owner and never
/// added to the shared Sprintlab library. The question body is stored as QuestionDataRequest JSON.
/// </summary>
public class TeacherQuestion
{
    public long Id { get; set; }
    public long OwnerUserId { get; set; }
    public Guid QuestionId { get; set; }
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string Subject { get; set; } = "";
    public int Unit { get; set; }
    public int Lesson { get; set; }
    public string QuestionType { get; set; } = "";
    public string QuestionText { get; set; } = "";
    /// <summary>Thinking-skill classification confirmed by the author (Remember..Create), optional.</summary>
    public string? ThinkingSkill { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
