namespace Shared.Requests.LiveGames;

public class AutoSelectQuestionsRequest
{
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public int Count { get; set; } = 10;
    public List<Guid> ExcludeQuestionIds { get; set; } = new();
}
