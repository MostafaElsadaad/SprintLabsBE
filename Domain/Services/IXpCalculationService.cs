using Domain.Enums;

using Shared.Requests;
using Shared.Responses;

namespace Domain.Services;

public interface IXpCalculationService
{
    int CalculateAnswerXp(IEnumerable<XpQuestionResult> orderedQuestionResults);

    int CalculateMatchResultXp(bool isWinner);

    int CalculateMissionXp(MissionDifficulty missionDifficulty);

    int CalculateTotalXp(int answerXp, int matchResultXp, int missionXp);

    XpCalculationResult CalculateXp(
        IEnumerable<XpQuestionResult> orderedQuestionResults,
        bool isWinner,
        MissionDifficulty? missionDifficulty = null);
}
