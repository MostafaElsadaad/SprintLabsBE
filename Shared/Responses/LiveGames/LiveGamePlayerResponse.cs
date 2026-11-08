namespace Shared.Responses.LiveGames;

public class LiveGamePlayerResponse
{
    public long PlayerProfileId { get; set; }
    public string DisplayName { get; set; } = "";
    public bool Connected { get; set; }
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    /// <summary>"correct", "wrong" or null (not answered yet) for the current question.</summary>
    public string? CurrentAnswer { get; set; }
    public int Rank { get; set; }
}
