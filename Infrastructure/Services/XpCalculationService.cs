using Domain.Enums;
using Domain.Services;

using Shared.Requests;
using Shared.Responses;

namespace Infrastructure.Services;

public class XpCalculationService : IXpCalculationService
{
    private const int CorrectAnswerBaseXp = 10;
    private const int WinnerXp = 50;
    private const int ParticipantXp = 20;

    public int CalculateAnswerXp(IEnumerable<XpQuestionResult> orderedQuestionResults)
    {
        ArgumentNullException.ThrowIfNull(orderedQuestionResults);

        var totalXp = 0;
        var currentStreak = 0;

        foreach (var questionResult in orderedQuestionResults)
        {
            if (!questionResult.IsCorrect)
            {
                currentStreak = 0;
                continue;
            }

            currentStreak++;
            totalXp += CalculateCorrectAnswerXp(currentStreak);
        }

        return totalXp;
    }

    public int CalculateMatchResultXp(bool isWinner)
    {
        return isWinner ? WinnerXp : ParticipantXp;
    }

    public int CalculateMissionXp(MissionDifficulty missionDifficulty)
    {
        return missionDifficulty switch
        {
            MissionDifficulty.Normal => 25,
            MissionDifficulty.Mid => 50,
            MissionDifficulty.Hard => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(missionDifficulty), missionDifficulty, null)
        };
    }

    public int CalculateTotalXp(int answerXp, int matchResultXp, int missionXp)
    {
        return answerXp + matchResultXp + missionXp;
    }

    public XpCalculationResult CalculateXp(
        IEnumerable<XpQuestionResult> orderedQuestionResults,
        bool isWinner,
        MissionDifficulty? missionDifficulty = null)
    {
        var answerXp = CalculateAnswerXp(orderedQuestionResults);
        var matchResultXp = CalculateMatchResultXp(isWinner);
        var missionXp = missionDifficulty.HasValue ? CalculateMissionXp(missionDifficulty.Value) : 0;

        return new XpCalculationResult
        {
            AnswerXp = answerXp,
            MatchResultXp = matchResultXp,
            MissionXp = missionXp,
            TotalXp = CalculateTotalXp(answerXp, matchResultXp, missionXp)
        };
    }

    private static int CalculateCorrectAnswerXp(int streak)
    {
        return streak switch
        {
            >= 10 => 30,
            >= 8 => 22,
            >= 6 => 18,
            >= 4 => 14,
            >= 2 => 12,
            _ => CorrectAnswerBaseXp
        };
    }
}
