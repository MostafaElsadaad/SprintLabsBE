using Domain.Enums;

namespace Domain.Models.QuestionData;

public class OrderingHistory
{
    public long HistoryId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int SelectedPosition { get; set; }
}
