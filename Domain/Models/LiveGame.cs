using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// A platform-admin hosted game. Any signed-in player may join with <see cref="Code"/>;
/// the question filter below decides which questions the game server plays.
/// </summary>
public class LiveGame
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public LiveGameStatus Status { get; set; } = LiveGameStatus.Lobby;
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public int? Term { get; set; }
    public string? Language { get; set; }
    public int MaxPlayers { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? ResultJson { get; set; }
}
