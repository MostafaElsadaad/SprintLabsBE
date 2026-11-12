namespace Shared.Requests.LiveGames;

public class TeacherQuestionFilter
{
    public string? Curriculum { get; set; }
    public int? Grade { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public string? QuestionType { get; set; }
    public string? Search { get; set; }
}
