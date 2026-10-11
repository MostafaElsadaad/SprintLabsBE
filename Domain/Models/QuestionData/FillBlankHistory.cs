using Domain.Enums;

namespace Domain.Models.QuestionData;

public class FillBlankHistory
{
    public long HistoryId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
}
