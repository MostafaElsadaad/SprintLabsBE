using Shared.Requests.QuestionData;

namespace Shared.Responses.LiveGames;

public class TeacherQuestionResponse
{
    public long Id { get; set; }
    public string? ThinkingSkill { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public QuestionDataRequest Question { get; set; } = new();
}
