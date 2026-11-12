namespace Shared.Requests.LiveGames;

public class LiveGameQuestionSetRequest
{
    public string Name { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public List<LiveGameQuestionItem> Questions { get; set; } = new();
}
