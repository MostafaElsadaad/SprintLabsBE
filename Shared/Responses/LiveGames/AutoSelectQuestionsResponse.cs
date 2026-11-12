using Shared.Requests.LiveGames;

namespace Shared.Responses.LiveGames;

public class AutoSelectQuestionsResponse
{
    public List<LiveGameQuestionItem> Questions { get; set; } = new();
    /// <summary>Library questions that match the filter and are not excluded.</summary>
    public int AvailableCount { get; set; }
    public string? Warning { get; set; }
}
