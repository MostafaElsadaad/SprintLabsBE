using Domain.Enums;

namespace Domain.Models.QuestionData;

public class DragDropOption
{
    public string OptionId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
}
