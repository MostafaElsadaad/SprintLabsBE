using Shared.Requests;
using Shared.Responses;

namespace Domain.Services;

public interface ILevelProgressionService
{
    int CalculateXpRequiredForLevel(int level);

    LevelProgressionResult CalculateProgression(LevelProgressionRequest request);

    IReadOnlyList<LevelUnlockResult> GetUnlocksForLevelUp(int oldLevel, int newLevel);
}
