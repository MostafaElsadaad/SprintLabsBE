using Domain.Enums;
using Domain.Services;

using FluentAssertions;

using Shared.Requests;

namespace Compass.Tests.Features.XpCalculationService;

public class XpCalculationServiceTests
{
    private readonly IXpCalculationService _service = new Infrastructure.Services.XpCalculationService();

    [Fact]
    public void CalculateAnswerXp_SingleCorrectAnswer_ReturnsBaseXp()
    {
        var result = _service.CalculateAnswerXp(Answers(true));

        result.Should().Be(10);
    }

    [Fact]
    public void CalculateAnswerXp_WrongAnswer_ReturnsZeroXp()
    {
        var result = _service.CalculateAnswerXp(Answers(false));

        result.Should().Be(0);
    }

    [Theory]
    [InlineData(2, 22)]
    [InlineData(4, 48)]
    [InlineData(6, 80)]
    [InlineData(8, 120)]
    [InlineData(10, 172)]
    public void CalculateAnswerXp_CorrectAnswerStreaks_ApplyThresholdMultiplier(
        int correctAnswerCount,
        int expectedXp)
    {
        var result = _service.CalculateAnswerXp(Answers(Enumerable.Repeat(true, correctAnswerCount).ToArray()));

        result.Should().Be(expectedXp);
    }

    [Fact]
    public void CalculateAnswerXp_WrongAnswerResetsStreak()
    {
        var result = _service.CalculateAnswerXp(Answers(true, true, false, true, true));

        result.Should().Be(44);
    }

    [Theory]
    [InlineData(true, 50)]
    [InlineData(false, 20)]
    public void CalculateMatchResultXp_WinnerOrParticipant_ReturnsExpectedXp(
        bool isWinner,
        int expectedXp)
    {
        var result = _service.CalculateMatchResultXp(isWinner);

        result.Should().Be(expectedXp);
    }

    [Theory]
    [InlineData(MissionDifficulty.Normal, 25)]
    [InlineData(MissionDifficulty.Mid, 50)]
    [InlineData(MissionDifficulty.Hard, 100)]
    public void CalculateMissionXp_Difficulty_ReturnsExpectedXp(
        MissionDifficulty difficulty,
        int expectedXp)
    {
        var result = _service.CalculateMissionXp(difficulty);

        result.Should().Be(expectedXp);
    }

    [Fact]
    public void CalculateTotalXp_Components_ReturnsSum()
    {
        var result = _service.CalculateTotalXp(answerXp: 44, matchResultXp: 50, missionXp: 25);

        result.Should().Be(119);
    }

    [Fact]
    public void CalculateXp_Components_ReturnsSeparateValuesAndTotal()
    {
        var result = _service.CalculateXp(Answers(true, true), isWinner: true, MissionDifficulty.Normal);

        result.AnswerXp.Should().Be(22);
        result.MatchResultXp.Should().Be(50);
        result.MissionXp.Should().Be(25);
        result.TotalXp.Should().Be(97);
    }

    private static XpQuestionResult[] Answers(params bool[] answers)
    {
        return answers
            .Select(isCorrect => new XpQuestionResult { IsCorrect = isCorrect })
            .ToArray();
    }
}
