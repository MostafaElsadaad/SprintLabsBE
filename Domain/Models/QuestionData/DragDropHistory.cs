using Domain.Enums;

namespace Domain.Models.QuestionData;

public class DragDropHistory
{
    public long HistoryId { get; set; }
    public int BlankNumber { get; set; }
    public string SelectedOptionId { get; set; } = string.Empty;
}
