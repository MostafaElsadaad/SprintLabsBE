namespace Shared.Requests.LiveGames;

public class CreateLiveGameRequest
{
    public string Title { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    /// <summary>Several subjects/units/lessons (empty or null = all). When null the single values above are used.</summary>
    public List<string>? Subjects { get; set; }
    public List<int>? Units { get; set; }
    public List<LiveGameLessonRef>? Lessons { get; set; }
    public int? Term { get; set; }
    public string? Language { get; set; }
    public int MaxPlayers { get; set; } = 8;
    /// <summary>Ordered questions for the game. When empty, the game uses the curriculum/grade filter instead.</summary>
    public List<LiveGameQuestionItem>? Questions { get; set; }
}
