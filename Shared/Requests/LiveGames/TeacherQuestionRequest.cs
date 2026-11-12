using Shared.Requests.QuestionData;

namespace Shared.Requests.LiveGames;

public class TeacherQuestionRequest
{
    public string? ThinkingSkill { get; set; }
    public QuestionDataRequest Question { get; set; } = new();
}
