namespace Shared.Requests.LiveGames;

public class LiveGameQuestionSetRequest
{
    public string Name { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    /// <summary>Several subjects/units/lessons (empty or null = all). When null the single values above are used.</summary>
    public List<string>? Subjects { get; set; }
    public List<int>? Units { get; set; }
    public List<LiveGameLessonRef>? Lessons { get; set; }
    public List<LiveGameQuestionItem> Questions { get; set; } = new();
}
