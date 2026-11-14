namespace Shared.Requests.LiveGames;

/// <summary>One lesson of one unit (lesson numbers restart in every unit).</summary>
public class LiveGameLessonRef
{
    public int Unit { get; set; }
    public int Lesson { get; set; }
}

/// <summary>
/// Which part of a curriculum/grade a live game or saved set covers. An empty list means "all":
/// no subjects = every subject, no units = every unit, no lessons = every lesson of the chosen units.
/// </summary>
public class LiveGameScope
{
    public List<string> Subjects { get; set; } = new();
    public List<int> Units { get; set; } = new();
    public List<LiveGameLessonRef> Lessons { get; set; } = new();
}
