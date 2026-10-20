namespace Shared.Responses;

public class MatchRegistrationResponse
{
    public long MatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool SupportsQuestionHistory { get; set; } = true;
}
