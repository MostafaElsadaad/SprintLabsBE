using Domain.Enums;

namespace Domain.Models.QuestionData;

public class TrueFalseHistory
{
    public long HistoryId { get; set; }
    public bool SelectedAnswer { get; set; }
}
