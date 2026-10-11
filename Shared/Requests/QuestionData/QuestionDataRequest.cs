namespace Shared.Requests.QuestionData;

public class QuestionDataRequest
{
    public Guid QuestionId { get; set; }
    public string Curriculum { get; set; } = string.Empty;
    public int Grade { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Term { get; set; }
    public int Unit { get; set; }
    public int Lesson { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public decimal TimerSeconds { get; set; } = 10m;
    public List<QuestionChoiceData> Choices { get; set; } = new();
    public bool? CorrectAnswer { get; set; }
    public List<string> AcceptedAnswers { get; set; } = new();
    public List<QuestionOrderingData> Items { get; set; } = new();
    public List<QuestionPairData> Pairs { get; set; } = new();
    public List<QuestionOptionData> Options { get; set; } = new();
    public List<QuestionBlankData> BlankAnswers { get; set; } = new();
}
