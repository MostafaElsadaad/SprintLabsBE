using Domain.Services;

using FluentAssertions;

using Shared.Requests;

namespace Compass.Tests.Features.LevelProgressionService;

public class LevelProgressionServiceTests
{
    private readonly ILevelProgressionService _service = new Infrastructure.Services.LevelProgressionService();

    [Fact]
    public void CalculateXpRequiredForLevel_LevelOne_ReturnsFormulaResult()
    {
        var result = _service.CalculateXpRequiredForLevel(1);

        result.Should().Be(150);
    }

    [Fact]
    public void CalculateXpRequiredForLevel_DecimalResult_UsesCeiling()
    {
        var expected = (int)Math.Ceiling(150 * Math.Pow(2, 1.25));

        var result = _service.CalculateXpRequiredForLevel(2);

        result.Should().Be(expected);
        result.Should().Be(357);
    }

    [Fact]
    public void CalculateProgression_NoExperience_StartsAtLevelOne()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest());

        result.OldLevel.Should().Be(1);
        result.NewLevel.Should().Be(1);
    }

    [Fact]
    public void CalculateProgression_NoXpGain_KeepsSameLevel()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 10,
            XpGained = 0
        });

        result.OldLevel.Should().Be(1);
        result.NewLevel.Should().Be(1);
        result.LeveledUp.Should().BeFalse();
        result.LevelsGained.Should().Be(0);
    }

    [Fact]
    public void CalculateProgression_EnoughXp_IncreasesLevelByOne()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 0,
            XpGained = 150
        });

        result.OldLevel.Should().Be(1);
        result.NewLevel.Should().Be(2);
        result.LevelsGained.Should().Be(1);
        result.LeveledUp.Should().BeTrue();
    }

    [Fact]
    public void CalculateProgression_LargeXpGain_SupportsMultipleLevelUps()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 0,
            XpGained = 1_200
        });

        result.OldLevel.Should().Be(1);
        result.NewLevel.Should().Be(4);
        result.LevelsGained.Should().Be(3);
        result.LeveledUp.Should().BeTrue();
    }

    [Fact]
    public void CalculateProgression_ReturnsOldAndNewValues()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 150,
            XpGained = 357
        });

        result.OldLevel.Should().Be(2);
        result.NewLevel.Should().Be(3);
        result.OldTotalXp.Should().Be(150);
        result.NewTotalXp.Should().Be(507);
        result.XpGained.Should().Be(357);
    }

    [Fact]
    public void CalculateProgression_ReturnsNextLevelXpRequirement()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 0,
            XpGained = 150
        });

        result.NextLevelXpRequirement.Should().Be(_service.CalculateXpRequiredForLevel(2));
    }

    [Fact]
    public void CalculateProgression_ReturnsEmptyFutureUnlockHook()
    {
        var result = _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 0,
            XpGained = 150
        });

        result.Unlocks.Should().NotBeNull();
        result.Unlocks.Should().BeEmpty();
        _service.GetUnlocksForLevelUp(1, 2).Should().BeEmpty();
    }

    [Fact]
    public void CalculateProgression_NegativeXpGain_ThrowsArgumentOutOfRange()
    {
        var act = () => _service.CalculateProgression(new LevelProgressionRequest
        {
            OldTotalXp = 0,
            XpGained = -1
        });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
