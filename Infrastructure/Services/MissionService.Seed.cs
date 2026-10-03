using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Requests;
using System.Text.Json;
namespace Infrastructure.Services;
public partial class MissionService
{
    public async Task<List<long>> SeedAsync(long adminUserId, CancellationToken ct)
    {
        await AdminAsync(adminUserId, ct);
        return await AtomicAsync(Array.Empty<long>(), async _ => {
            var item = await _context.Items.SingleOrDefaultAsync(x => x.ItemKey == "mission_seed_hat", ct);
            if (item == null) { item = new Item { ItemKey = "mission_seed_hat", Name = "Mission Hat", IconKey = "mission_seed_hat", PrefabKey = "mission_seed_hat" }; _context.Items.Add(item); await _context.SaveChangesAsync(ct); }
            var box = await _context.ShopProducts.SingleOrDefaultAsync(x => x.Name == "Mission Seed Box", ct);
            if (box == null) { box = new ShopProduct { Name = "Mission Seed Box", BoxRewards = new List<BoxRewardEntry> { new() { ItemId = item.Id, Weight = 1, Quantity = 1 } } }; _context.ShopProducts.Add(box); await _context.SaveChangesAsync(ct); }
            var examples = new List<MissionTemplateRequest> {
                new() { MissionKey = "seed_use_shield", Title = "Shield Explorer", Description = "Use Shield once.", Category = "Exploration", PeriodType = "Weekly", EventType = "PowerupUsed", ProgressType = "Boolean", TargetValue = 1,
                    Conditions = new() { ["powerupKey"] = JsonSerializer.SerializeToElement("shield") }, Rewards = new() { new() { RewardType = "XP", Amount = 25 } } },
                new() { MissionKey = "seed_answer_10", Title = "Practice Makes Progress", Description = "Answer ten questions correctly.", Category = "Progress", PeriodType = "Weekly", EventType = "CorrectAnswer", ProgressType = "Count", TargetValue = 10,
                    Rewards = new() { new() { RewardType = "Coins", Amount = 100 } } },
                new() { MissionKey = "seed_win_3", Title = "Triple Winner", Description = "Win three matches.", Category = "Competitive", PeriodType = "Monthly", EventType = "MatchWon", ProgressType = "Count", TargetValue = 3,
                    Conditions = new() { ["position"] = JsonSerializer.SerializeToElement(1) }, Rewards = new() { new() { RewardType = "Box", ShopProductId = box.Id } } },
                new() { MissionKey = "seed_repeat_dice", Title = "Lucky Double", Description = "Roll the same dice value twice consecutively.", Category = "Lucky", PeriodType = "Open", EventType = "DiceRolled", ProgressType = "Streak", ProgressField = "diceValue", RepeatValue = true, TargetValue = 2,
                    Rewards = new() { new() { RewardType = "XP", Amount = 50 } } }
            };
            var templates = new List<MissionTemplate>();
            foreach (var example in examples)
            {
                var existing = await _context.MissionTemplates.SingleOrDefaultAsync(x => x.MissionKey == example.MissionKey, ct);
                templates.Add(existing ?? await AddTemplateAsync(example, ct)); await _context.SaveChangesAsync(ct);
            }
            var now = DateTime.UtcNow; var monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
            var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var activations = new List<MissionActivationRequest> {
                new() { Name = $"Seed weekly {monday:yyyy-MM-dd}", PeriodType = "Weekly", StartsAt = monday, EndsAt = monday.AddDays(7), RandomMissionCount = 1,
                    Items = new() { new() { MissionTemplateId = templates[0].Id }, new() { MissionTemplateId = templates[1].Id, AssignmentMode = "RandomPool" } } },
                new() { Name = $"Seed monthly {month:yyyy-MM}", PeriodType = "Monthly", StartsAt = month, EndsAt = month.AddMonths(1), Items = new() { new() { MissionTemplateId = templates[2].Id } } },
                new() { Name = "Seed open", PeriodType = "Open", StartsAt = now.AddMinutes(-1), Items = new() { new() { MissionTemplateId = templates[3].Id } } }
            };
            var ids = new List<long>();
            foreach (var activation in activations) {
                var a = await _context.MissionActivations.SingleOrDefaultAsync(x => x.Name == activation.Name, ct) ?? await AddActivationAsync(activation, ct);
                await _context.SaveChangesAsync(ct); ids.Add(a.Id);
            }
            return ids;
        }, ct);
    }
}
