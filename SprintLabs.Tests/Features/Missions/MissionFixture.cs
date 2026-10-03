using Compass.Tests.Features.MatchProgression;
using Infrastructure.DataAccess;
using Infrastructure.Services;
using Shared.Requests;
using Shared.Responses;
using System.Text.Json;
namespace Compass.Tests.Features.Missions;
internal sealed class MissionFixture : IDisposable
{
    public ProgressionFixture Base { get; }
    public ApplicationDbContext Context => Base.Context;
    public MissionService Service { get; }
    public MissionFixture(ApplicationDbContext? context = null) { Base = new(context); Service = new(Context, new Infrastructure.Services.LevelProgressionService()); }
    public Task SeedAsync() => Base.SeedAsync();
    public async Task<PlayerMissionResponse> AssignAsync(string type = "Count", int target = 2, string evt = "CorrectAnswer", bool matchScoped = false,
        bool repeat = false, string? reset = null, Dictionary<string, JsonElement>? conditions = null)
    {
        var id = await Service.CreateTemplateAsync(40, new() { MissionKey = Guid.NewGuid().ToString(), Title = "Test mission", Description = "Test",
            Category = "Progress", PeriodType = "Open", EventType = evt, ProgressType = type, TargetValue = target,
            MatchScoped = matchScoped, RepeatValue = repeat, ResetEventType = reset, Conditions = conditions ?? new(),
            Rewards = new() { new() { RewardType = "XP", Amount = 50 }, new() { RewardType = "Coins", Amount = 20 } } }, default);
        await Service.CreateActivationAsync(40, new() { Name = Guid.NewGuid().ToString(), PeriodType = "Open", StartsAt = DateTime.UtcNow.AddDays(-1),
            Items = new() { new() { MissionTemplateId = id } } }, default);
        return (await Service.GetMyAsync(1, default)).Single(x => x.MissionKey == Context.MissionTemplates.Single(t => t.Id == id).MissionKey);
    }
    public MissionEventRequest Event(string type = "CorrectAnswer", object? value = null, string? eventId = null) => new()
    {
        EventId = eventId ?? Guid.NewGuid().ToString(), PlayerProfileId = 101, EventType = type, OccurredAt = DateTime.UtcNow.AddSeconds(-5),
        EventData = value == null ? new() : new() { ["value"] = JsonSerializer.SerializeToElement(value) }
    };
    public void Dispose() => Base.Dispose();
}
