namespace Infrastructure.Services;
internal class MissionProgressState
{
    public long? MatchId { get; set; }
    public string? LastValue { get; set; }
    public HashSet<string> UniqueValues { get; set; } = new(StringComparer.Ordinal);
}
