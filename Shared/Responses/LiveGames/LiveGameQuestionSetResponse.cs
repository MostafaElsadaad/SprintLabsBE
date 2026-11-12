using Shared.Requests.LiveGames;

namespace Shared.Responses.LiveGames;

public class LiveGameQuestionSetResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<LiveGameQuestionItem> Questions { get; set; } = new();
}
