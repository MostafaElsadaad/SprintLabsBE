namespace Shared.Responses;

public class LevelProgressionResult
{
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldTotalXp { get; set; }
    public int NewTotalXp { get; set; }
    public int XpGained { get; set; }
    public bool LeveledUp { get; set; }
    public int LevelsGained { get; set; }
    public int NextLevelXpRequirement { get; set; }
    public IReadOnlyList<LevelUnlockResult> Unlocks { get; set; } = Array.Empty<LevelUnlockResult>();
}
