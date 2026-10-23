using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using System.Text.Json;

namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<List<long>> SeedGameCatalogueAsync(long adminUserId, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        return await AtomicAsync(Array.Empty<long>(), async _ => {
            var now = DateTime.UtcNow;
            var week = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var season = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var result = new List<long>();
            foreach (var period in new[] { "Weekly", "Seasonal" })
            {
                var seasonal = period == "Seasonal";
                var templates = new List<MissionTemplate>();
                foreach (var definition in GameCatalogue(period))
                {
                    var template = await _context.MissionTemplates.SingleOrDefaultAsync(x => x.MissionKey == definition.MissionKey, ct)
                        ?? await AddTemplateAsync(definition, ct);
                    templates.Add(template);
                    await _context.SaveChangesAsync(ct);
                }
                // Explicit repeat-safe seed: current and next periods, no hidden startup mutation.
                for (var offset = 0; offset < 2; offset++)
                {
                    var start = seasonal ? season.AddMonths(offset * 3) : week.AddDays(offset * 7);
                    var end = seasonal ? start.AddMonths(3) : start.AddDays(7);
                    var name = $"SprintLab {period} {start:yyyy-MM-dd} v1";
                    var activation = await _context.MissionActivations.SingleOrDefaultAsync(x => x.Name == name, ct)
                        ?? await AddActivationAsync(new() { Name = name, PeriodType = period, StartsAt = start, EndsAt = end,
                            AutoClaimCompletedOnReset = true,
                            Items = templates.Select((t, i) => new MissionActivationItemRequest {
                                MissionTemplateId = t.Id, AssignmentMode = "Fixed", SortOrder = i }).ToList() }, ct);
                    await _context.SaveChangesAsync(ct);
                    result.Add(activation.Id);
                }
            }
            return result;
        }, ct);
    }

    private static List<MissionTemplateRequest> GameCatalogue(string period)
    {
        var seasonal = period == "Seasonal";
        var result = new List<MissionTemplateRequest>();
        void Add(string key, string title, string evt, int weekly, int season, string mode = "Count",
            string? field = null, string? conditionKey = null, object? condition = null,
            string? reset = null, bool scoped = false, bool repeat = false)
        {
            var target = seasonal ? season : weekly;
            var t = new MissionTemplateRequest {
                MissionKey = $"game_v1_{period.ToLowerInvariant()}_{key}", Title = title,
                Description = $"{title}: reach {target} during this {period.ToLowerInvariant()} period.",
                Category = evt.StartsWith("Match") ? "Competitive" : evt == "DiceRolled" ? "Lucky" : "Progress",
                PeriodType = period, EventType = evt, TargetValue = target, ProgressType = mode,
                ProgressField = field ?? "value", ResetEventType = reset, MatchScoped = scoped, RepeatValue = repeat,
                Rewards = new() { new() { RewardType = "XP", Amount = seasonal ? 100 : 25 },
                    new() { RewardType = "Coins", Amount = seasonal ? 200 : 50 } }
            };
            if (conditionKey != null) t.Conditions[conditionKey] = JsonSerializer.SerializeToElement(condition);
            result.Add(t);
        }
        Add("correct", "Correct answers", "CorrectAnswer", 10, 100);
        foreach (var type in new[] { "MCQ", "Ordering", "MatchingPairs", "FillBlank", "TrueOrFalse", "DragAndDrop" })
            Add("correct_" + type.ToLowerInvariant(), type + " correct answers", "CorrectAnswer", 5, 20,
                conditionKey: "questionType", condition: type);
        Add("types", "Different question types", "CorrectAnswer", 3, 6, "UniqueCount", "questionType");
        Add("one_match", "Correct answers in one match", "CorrectAnswer", 5, 10, scoped: true);
        Add("answer_streak", "Consecutive correct answers", "CorrectAnswer", 3, 5, "Streak", reset: "WrongAnswer");
        Add("quick", "Quick correct answers", "CorrectAnswer", 5, 25, conditionKey: "fast", condition: true);
        Add("matches", "Complete matches", "MatchCompleted", 3, 20);
        Add("wins", "Win matches", "MatchWon", 1, 5);
        Add("ranked", "Complete ranked matches", "MatchCompleted", 3, 15, conditionKey: "matchType", condition: "Ranked");
        Add("win_streak", "Consecutive match wins", "MatchCompleted", 2, 3, "Streak", conditionKey: "won", condition: true);
        Add("podium", "Finish in the top three", "MatchCompleted", 3, 10, conditionKey: "topThree", condition: true);
        Add("monsters", "Defeat monsters", "MonsterDefeated", 5, 30);
        foreach (var monster in new[] { "cancel_move", "fog", "move_back_two", "move_back_four", "skip_turn", "slow" })
            Add("monster_" + monster, "Defeat " + monster.Replace('_', ' '), "MonsterDefeated", 1, 5,
                conditionKey: "monsterKey", condition: monster);
        Add("monster_types", "Different monsters defeated", "MonsterDefeated", 3, 6, "UniqueCount", "monsterKey");
        Add("monster_streak", "Consecutive monster victories", "MonsterDefeated", 3, 5, "Streak", reset: "MonsterFightLost");
        Add("rolls", "Roll dice", "DiceRolled", 20, 100);
        Add("sixes", "Roll six", "DiceRolled", 3, 15, conditionKey: "diceValue", condition: 6);
        Add("repeat_dice", "Repeat a dice value consecutively", "DiceRolled", 2, 3, "Streak", "diceValue", scoped: true, repeat: true);
        return result;
    }
}
