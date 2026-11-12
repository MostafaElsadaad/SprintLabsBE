using Shared.Requests.QuestionData;

namespace Shared.Requests.LiveGames;

/// <summary>
/// One question in a game or saved set. Source: "library" (unchanged Sprintlab question),
/// "edited" (a game-only copy of a library question) or "mine" (from My Questions or written now).
/// </summary>
public class LiveGameQuestionItem
{
    public string Source { get; set; } = "library";
    public long? TeacherQuestionId { get; set; }
    public string? ThinkingSkill { get; set; }
    public QuestionDataRequest Question { get; set; } = new();
}
