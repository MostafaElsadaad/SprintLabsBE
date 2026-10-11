using Domain.Enums;

namespace Domain.Models.QuestionData;

public class MCQChoice
{
    public string ChoiceId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
