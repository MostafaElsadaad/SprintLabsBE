using Domain.Enums;

namespace Domain.Models.QuestionData;

public class TrueFalseAnswer
{
    public Guid QuestionId { get; set; }
    public bool CorrectAnswer { get; set; }
}
