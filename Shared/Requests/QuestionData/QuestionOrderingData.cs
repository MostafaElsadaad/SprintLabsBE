namespace Shared.Requests.QuestionData;

public class QuestionOrderingData
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemText { get; set; } = string.Empty;
    public int? CorrectPosition { get; set; }
}
