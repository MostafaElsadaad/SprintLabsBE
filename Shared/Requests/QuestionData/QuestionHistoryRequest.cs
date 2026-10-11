namespace Shared.Requests.QuestionData;

public class QuestionHistoryRequest
{
    public long PlayerId { get; set; }
    public Guid QuestionId { get; set; }
    public long MatchId { get; set; }
    [System.Text.Json.Serialization.JsonRequired]
    public decimal TimeTakenSeconds { get; set; }
    public string? SelectedChoiceId { get; set; }
    public bool? SelectedAnswer { get; set; }
    public string? AnswerText { get; set; }
    public List<OrderingResponseData> Ordering { get; set; } = new();
    public List<MatchingResponseData> Matching { get; set; } = new();
    public List<DragDropResponseData> DragDrop { get; set; } = new();
}
