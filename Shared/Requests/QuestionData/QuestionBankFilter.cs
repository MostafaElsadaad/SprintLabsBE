namespace Shared.Requests.QuestionData;

public class QuestionBankFilter
{
    public int Grade { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public string? Curriculum { get; set; }
    public string? Language { get; set; }
    public string? Subject { get; set; }
    public int? Term { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
