namespace Shared.Responses;

public sealed class JourneyOptionResponse
{
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string Subject { get; set; } = "";
    public int Unit { get; set; }
}
