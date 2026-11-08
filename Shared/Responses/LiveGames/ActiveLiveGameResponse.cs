namespace Shared.Responses.LiveGames;

/// <summary>What the game server needs to open, start or close a live game room.</summary>
public class ActiveLiveGameResponse
{
    public string Code { get; set; } = "";
    public string Status { get; set; } = "";
    public int MaxPlayers { get; set; }
    public int BotCount { get; set; }
}
