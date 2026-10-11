namespace Shared.Requests.QuestionData;

public class QuestionChoiceData
{
    public string ChoiceId { get; set; } = string.Empty;
    public string ChoiceText { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
}
