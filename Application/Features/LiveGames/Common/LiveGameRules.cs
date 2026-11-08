namespace Application.Features.LiveGames.Common;

internal static class LiveGameRules
{
    /// <summary>
    /// The game board places at most 8 pawns on one tile without a client error (PlayerMovement offsets),
    /// and every player starts on the same tile.
    /// </summary>
    public const int MaxPlayersLimit = 8;
    public const int DefaultMaxPlayers = 8;
    public const int CodeLength = 5;
    public const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>The game ends after 10 rounds with one question per round; send spare questions.</summary>
    public const int QuestionsSentToServer = 20;
    public const int QuestionPoolSize = 200;

    /// <summary>Ended games stay visible to the game server briefly so it can close their rooms.</summary>
    public static readonly TimeSpan EndedGameServerWindow = TimeSpan.FromMinutes(10);
}
