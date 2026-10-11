using Domain.Enums;

namespace Domain.Models.QuestionData;

public class QuestionHistory
{
    public long HistoryId { get; set; }
    public long PlayerId { get; set; }
    public Guid QuestionId { get; set; }
    public long MatchId { get; set; }
    public decimal TimeTakenSeconds { get; set; }
    public List<MCQHistory> MCQHistoryRows { get; set; } = new();
    public List<TrueFalseHistory> TrueFalseHistoryRows { get; set; } = new();
    public List<FillBlankHistory> FillBlankHistoryRows { get; set; } = new();
    public List<OrderingHistory> OrderingHistoryRows { get; set; } = new();
    public List<MatchingHistory> MatchingHistoryRows { get; set; } = new();
    public List<DragDropHistory> DragDropHistoryRows { get; set; } = new();
}
