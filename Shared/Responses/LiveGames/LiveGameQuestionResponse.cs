namespace Shared.Responses.LiveGames;

public class LiveGameQuestionResponse
{
    public int Number { get; set; }
    public string? Type { get; set; }
    public string? Text { get; set; }
    public DateTime OpenedAt { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
}
