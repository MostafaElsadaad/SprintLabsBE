using System.Text.Json;

namespace Shared.Responses.LiveGames;

/// <summary>Same payload shape as GET api/v1/Question so the game's question loader can read it.</summary>
public class LiveGameQuestionsResponse
{
    public JsonElement PayloadJson { get; set; }
}
