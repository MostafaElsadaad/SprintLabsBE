using Domain.Services;

using Shared.Requests;
using Shared.Responses;

namespace Infrastructure.Services;

public class LevelProgressionService : ILevelProgressionService
{
    private const int MinimumLevel = 1;

    public int CalculateXpRequiredForLevel(int level)
    {
        var safeLevel = Math.Max(MinimumLevel, level);
        return (int)Math.Ceiling(150 * Math.Pow(safeLevel, 1.25));
    }

    public LevelProgressionResult CalculateProgression(LevelProgressionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OldTotalXp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.OldTotalXp, "Old total XP must not be negative.");
        }

        if (request.XpGained < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.XpGained, "XP gained must not be negative.");
        }

        var oldLevel = CalculateLevelFromTotalXp(request.OldTotalXp);
        var newTotalXp = request.OldTotalXp + request.XpGained;
        var newLevel = CalculateLevelFromTotalXp(newTotalXp);
        var levelsGained = Math.Max(0, newLevel - oldLevel);

        return new LevelProgressionResult
        {
            OldLevel = oldLevel,
            NewLevel = newLevel,
            OldTotalXp = request.OldTotalXp,
            NewTotalXp = newTotalXp,
            XpGained = request.XpGained,
            LeveledUp = levelsGained > 0,
            LevelsGained = levelsGained,
            NextLevelXpRequirement = CalculateXpRequiredForLevel(newLevel),
            Unlocks = GetUnlocksForLevelUp(oldLevel, newLevel)
        };
    }

    public IReadOnlyList<LevelUnlockResult> GetUnlocksForLevelUp(int oldLevel, int newLevel)
    {
        return Array.Empty<LevelUnlockResult>();
    }

    private int CalculateLevelFromTotalXp(int totalXp)
    {
        var level = MinimumLevel;
        var remainingXp = totalXp;

        while (remainingXp >= CalculateXpRequiredForLevel(level))
        {
            remainingXp -= CalculateXpRequiredForLevel(level);
            level++;
        }

        return level;
    }
}
