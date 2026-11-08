using System.Collections.Concurrent;
using Domain.Services;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Infrastructure.Services;

public sealed class LiveGameStateStore : ILiveGameStateStore
{
    private const int MaxRememberedEvents = 5000;
    private const int MaxActivity = 40;
    private readonly ConcurrentDictionary<long, GameState> games = new();
    private long gameServerLastSeenTicks;

    public DateTime? GameServerLastSeenAt
    {
        get
        {
            long ticks = Interlocked.Read(ref gameServerLastSeenTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public void MarkGameServerSeen() => Interlocked.Exchange(ref gameServerLastSeenTicks, DateTime.UtcNow.Ticks);

    public bool Apply(long liveGameId, LiveGameEventRequest liveEvent)
    {
        if (liveEvent == null || string.IsNullOrWhiteSpace(liveEvent.EventId) || liveEvent.EventId.Length > 100)
            return false;
        GameState state = games.GetOrAdd(liveGameId, _ => new GameState());
        lock (state)
        {
            if (state.SeenEvents.Contains(liveEvent.EventId)) return false;
            if (!ApplyLocked(state, liveEvent)) return false;
            state.SeenEvents.Add(liveEvent.EventId);
            state.SeenOrder.Enqueue(liveEvent.EventId);
            while (state.SeenOrder.Count > MaxRememberedEvents)
                state.SeenEvents.Remove(state.SeenOrder.Dequeue());
            return true;
        }
    }

    public int JoinedCount(long liveGameId)
    {
        if (!games.TryGetValue(liveGameId, out GameState? state)) return 0;
        lock (state) return state.Players.Values.Count(p => p.Connected);
    }

    public void Fill(long liveGameId, LiveGameResponse response)
    {
        DateTime? seen = GameServerLastSeenAt;
        response.GameServerLastSeenAt = seen;
        response.GameServerConnected = seen.HasValue && DateTime.UtcNow - seen.Value < TimeSpan.FromSeconds(15);
        if (!games.TryGetValue(liveGameId, out GameState? state)) return;
        lock (state)
        {
            response.Players = RankedPlayers(state);
            response.QuestionsAsked = state.QuestionsAsked;
            response.CurrentQuestion = state.CurrentQuestion == null ? null : new LiveGameQuestionResponse
            {
                Number = state.CurrentQuestion.Number,
                Type = state.CurrentQuestion.Type,
                Text = state.CurrentQuestion.Text,
                OpenedAt = state.CurrentQuestion.OpenedAt,
                CorrectCount = state.CurrentQuestion.CorrectCount,
                WrongCount = state.CurrentQuestion.WrongCount
            };
            response.Activity = state.Activity
                .Select(a => new LiveGameActivityResponse { At = a.At, Message = a.Message })
                .ToList();
        }
    }

    public List<LiveGamePlayerResponse> Standings(long liveGameId)
    {
        if (!games.TryGetValue(liveGameId, out GameState? state)) return new List<LiveGamePlayerResponse>();
        lock (state) return RankedPlayers(state);
    }

    private static bool ApplyLocked(GameState state, LiveGameEventRequest e)
    {
        DateTime at = e.OccurredAt == default ? DateTime.UtcNow : e.OccurredAt;
        PlayerState? player = null;
        if (e.PlayerProfileId is > 0)
        {
            if (!state.Players.TryGetValue(e.PlayerProfileId.Value, out player) && e.Type == "player_joined")
            {
                player = new PlayerState { PlayerProfileId = e.PlayerProfileId.Value, JoinOrder = state.NextJoinOrder++ };
                state.Players[player.PlayerProfileId] = player;
            }
            string? displayName = Trim(e.DisplayName, 64);
            if (player != null && displayName != null) player.DisplayName = displayName;
        }

        string name = player != null && player.DisplayName.Length > 0 ? player.DisplayName : "A player";
        switch (e.Type)
        {
            case "player_joined":
                if (player == null) return false;
                player.Connected = true;
                AddActivity(state, at, name + " joined");
                return true;
            case "player_left":
                if (player == null) return false;
                if (state.Started) player.Connected = false;
                else state.Players.Remove(player.PlayerProfileId);
                AddActivity(state, at, name + " left");
                return true;
            case "player_disconnected":
                if (player == null) return false;
                player.Connected = false;
                AddActivity(state, at, name + " lost connection");
                return true;
            case "player_reconnected":
                if (player == null) return false;
                player.Connected = true;
                AddActivity(state, at, name + " reconnected");
                return true;
            case "bot_joined":
                AddActivity(state, at, (Trim(e.DisplayName, 64) ?? "A bot") + " (bot) joined");
                return true;
            case "match_started":
                state.Started = true;
                AddActivity(state, at, "The game started");
                return true;
            case "start_failed":
                AddActivity(state, at, "The game server could not start the game because nobody is in the room");
                return true;
            case "question_opened":
                int number = e.QuestionNumber is > 0 ? e.QuestionNumber.Value : state.QuestionsAsked + 1;
                state.CurrentQuestion = new QuestionState
                {
                    Number = number,
                    Type = Trim(e.QuestionType, 32),
                    Text = Trim(e.QuestionText, 500),
                    OpenedAt = at
                };
                state.QuestionsAsked = Math.Max(state.QuestionsAsked, number);
                foreach (PlayerState p in state.Players.Values) p.CurrentAnswer = null;
                AddActivity(state, at, "Question " + number + " opened");
                return true;
            case "answer_graded":
                if (player == null || e.IsCorrect == null) return false;
                bool correct = e.IsCorrect.Value;
                player.CurrentAnswer = correct ? "correct" : "wrong";
                if (correct) player.CorrectAnswers++;
                else player.WrongAnswers++;
                if (e.Score is >= 0) player.Score = e.Score.Value;
                if (state.CurrentQuestion != null)
                {
                    if (correct) state.CurrentQuestion.CorrectCount++;
                    else state.CurrentQuestion.WrongCount++;
                }
                AddActivity(state, at, name + (correct ? " answered correctly" : " answered wrong"));
                return true;
            case "score_changed":
                if (player == null || e.Score is not >= 0) return false;
                player.Score = e.Score.Value;
                return true;
            case "player_result":
                if (player == null) return false;
                if (e.Score is >= 0) player.Score = e.Score.Value;
                if (e.Rank is > 0) player.FinalRank = e.Rank.Value;
                return true;
            case "match_ended":
                state.Ended = true;
                state.CurrentQuestion = null;
                AddActivity(state, at, "The game ended");
                return true;
            default:
                return false;
        }
    }

    private static List<LiveGamePlayerResponse> RankedPlayers(GameState state)
    {
        var ordered = state.Players.Values
            .OrderBy(p => p.FinalRank ?? int.MaxValue)
            .ThenByDescending(p => p.Score)
            .ThenByDescending(p => p.CorrectAnswers)
            .ThenBy(p => p.JoinOrder)
            .ToList();
        var result = new List<LiveGamePlayerResponse>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            PlayerState p = ordered[i];
            result.Add(new LiveGamePlayerResponse
            {
                PlayerProfileId = p.PlayerProfileId,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? "Player " + p.PlayerProfileId : p.DisplayName,
                Connected = p.Connected,
                Score = p.Score,
                CorrectAnswers = p.CorrectAnswers,
                WrongAnswers = p.WrongAnswers,
                CurrentAnswer = p.CurrentAnswer,
                Rank = p.FinalRank ?? i + 1
            });
        }
        return result;
    }

    private static void AddActivity(GameState state, DateTime at, string message)
    {
        state.Activity.Insert(0, new ActivityState { At = at, Message = message });
        if (state.Activity.Count > MaxActivity) state.Activity.RemoveAt(state.Activity.Count - 1);
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length > max ? value[..max] : value;
    }

    private sealed class GameState
    {
        public readonly HashSet<string> SeenEvents = new(StringComparer.Ordinal);
        public readonly Queue<string> SeenOrder = new();
        public readonly Dictionary<long, PlayerState> Players = new();
        public readonly List<ActivityState> Activity = new();
        public QuestionState? CurrentQuestion;
        public int QuestionsAsked;
        public int NextJoinOrder;
        public bool Started;
        public bool Ended;
    }

    private sealed class PlayerState
    {
        public long PlayerProfileId;
        public string DisplayName = "";
        public int JoinOrder;
        public bool Connected;
        public int Score;
        public int CorrectAnswers;
        public int WrongAnswers;
        public string? CurrentAnswer;
        public int? FinalRank;
    }

    private sealed class QuestionState
    {
        public int Number;
        public string? Type;
        public string? Text;
        public DateTime OpenedAt;
        public int CorrectCount;
        public int WrongCount;
    }

    private sealed class ActivityState
    {
        public DateTime At;
        public string Message = "";
    }
}
