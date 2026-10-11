using Domain.Enums;

namespace Domain.Models.QuestionData;

public class MatchingHistory
{
    public long HistoryId { get; set; }
    public string LeftPairId { get; set; } = string.Empty;
    public string SelectedRightPairId { get; set; } = string.Empty;
}
