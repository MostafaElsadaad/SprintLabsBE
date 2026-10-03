namespace Shared.Requests;

public class CompleteMatchRequest
{
    public string MatchType { get; set; } = string.Empty;
    public long? CommunityId { get; set; }
    public string? MirrorRoomId { get; set; }
    public DateTime EndedAt { get; set; }
    public List<MatchPlayerResultRequest> Players { get; set; } = new();
    // Chronological for each player, including wrong/unanswered questions to break streaks.
    public List<MatchAnswerRequest> QuestionResults { get; set; } = new();
}
