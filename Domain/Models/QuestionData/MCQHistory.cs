using Domain.Enums;

namespace Domain.Models.QuestionData;

public class MCQHistory
{
    public long HistoryId { get; set; }
    public string SelectedChoiceId { get; set; } = string.Empty;
}
