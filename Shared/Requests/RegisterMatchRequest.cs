namespace Shared.Requests;

public class RegisterMatchRequest
{
    public string MatchCode { get; set; } = string.Empty;
    public string? MirrorRoomId { get; set; }
    public string MatchType { get; set; } = string.Empty;
    public long? CommunityId { get; set; }
    public DateTime StartedAt { get; set; }
    // Includes connectionless bots. Only authenticated humans appear in PlayerProfileIds.
    public int TotalPlayers { get; set; }
    public List<long> PlayerProfileIds { get; set; } = new();
}
