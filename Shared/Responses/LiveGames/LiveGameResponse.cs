namespace Shared.Responses.LiveGames;

public class LiveGameResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Curriculum { get; set; } = "";
    public int Grade { get; set; }
    public string? Subject { get; set; }
    public int? Unit { get; set; }
    public int? Lesson { get; set; }
    public int? Term { get; set; }
    public string? Language { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool GameServerConnected { get; set; }
    public DateTime? GameServerLastSeenAt { get; set; }
    public int QuestionsAsked { get; set; }
    public LiveGameQuestionResponse? CurrentQuestion { get; set; }
    public List<LiveGamePlayerResponse> Players { get; set; } = new();
    public List<LiveGameActivityResponse> Activity { get; set; } = new();
}
