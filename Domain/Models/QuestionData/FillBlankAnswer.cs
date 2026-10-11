using Domain.Enums;

namespace Domain.Models.QuestionData;

public class FillBlankAnswer
{
    public Guid QuestionId { get; set; }
    public string AcceptedAnswer { get; set; } = string.Empty;
}
