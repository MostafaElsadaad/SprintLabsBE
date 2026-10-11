using Domain.Enums;

namespace Domain.Models.QuestionData;

public class MatchingPair
{
    public string PairId { get; set; } = string.Empty;
    public Guid QuestionId { get; set; }
    public string LeftText { get; set; } = string.Empty;
    public string RightText { get; set; } = string.Empty;
}
