using Shared.Requests.LiveGames;

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
    public List<string> Subjects { get; set; } = new();
    public List<int> Units { get; set; } = new();
    public List<LiveGameLessonRef> Lessons { get; set; } = new();
    public int? Term { get; set; }
    public string? Language { get; set; }
    public int MaxPlayers { get; set; }
    public int BotCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool GameServerConnected { get; set; }
    public DateTime? GameServerLastSeenAt { get; set; }
    public int QuestionsAsked { get; set; }
    /// <summary>Questions chosen for the game (0 for legacy filter games, which play 10 rounds).</summary>
    public int QuestionCount { get; set; }
    public List<LiveGameQuestionItem> Questions { get; set; } = new();
    public LiveGameQuestionResponse? CurrentQuestion { get; set; }
    public List<LiveGamePlayerResponse> Players { get; set; } = new();
    public List<LiveGameActivityResponse> Activity { get; set; } = new();
    /// <summary>Round history: each question asked and its correct/wrong counts. Saved with the final standings.</summary>
    public List<LiveGameQuestionResponse> Rounds { get; set; } = new();
}
