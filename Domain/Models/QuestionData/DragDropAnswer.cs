using Domain.Enums;

namespace Domain.Models.QuestionData;

public class DragDropAnswer
{
    public Guid QuestionId { get; set; }
    public int BlankNumber { get; set; }
    public string CorrectText { get; set; } = string.Empty;
}
