using Domain.Enums;

namespace Domain.Models.QuestionData;

public class OrderingItem
{
    public string ItemId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public string ItemText { get; set; } = string.Empty;
    public int CorrectPosition { get; set; }
}
