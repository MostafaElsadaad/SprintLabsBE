using Domain.Services;

using FluentAssertions;

namespace Compass.Tests.Features.RpRankCalculationService;

using DomainMatchType = Domain.Enums.MatchType;
using DomainRankTier = Domain.Enums.RankTier;

public class RpRankCalculationServiceTests
{
    private readonly IRpRankCalculationService _service = new Infrastructure.Services.RpRankCalculationService();

    [Theory]
    [InlineData(1, 5, 1d)]
    [InlineData(3, 5, 0.5d)]
    [InlineData(5, 5, 0d)]
    public void CalculatePlayerPercentile_Position_ReturnsExpectedPercentile(
        int position,
        int totalPlayers,
        double expectedPercentile)
    {
        var result = _service.CalculatePlayerPercentile(position, totalPlayers);

        result.Should().Be(expectedPercentile);
    }

    [Theory]
    [InlineData(1d, 30d)]
    [InlineData(0.5d, 0d)]
    [InlineData(0d, -30d)]
    public void CalculateBaseRp_Percentile_ReturnsExpectedBaseRp(double percentile, double expectedBaseRp)
    {
        var result = _service.CalculateBaseRp(percentile);

        result.Should().Be(expectedBaseRp);
    }

    [Fact]
    public void CalculateRpAndRank_RankedFirstPlace_ReturnsPositiveRp()
    {
        var result = _service.CalculateRpAndRank(100, DomainMatchType.Ranked, position: 1, totalPlayers: 5);

        result.OldRp.Should().Be(100);
        result.RpChange.Should().Be(30);
        result.NewRp.Should().Be(130);
        result.OldRankTier.Should().Be((int)DomainRankTier.Seeker);
        result.NewRankTier.Should().Be((int)DomainRankTier.Seeker);
        result.IsRanked.Should().BeTrue();
    }

    [Fact]
    public void CalculateRpAndRank_RankedLastPlace_ReturnsNegativeRpBeforeClamping()
    {
        var result = _service.CalculateRpAndRank(1_000, DomainMatchType.Ranked, position: 5, totalPlayers: 5);

        result.RpChange.Should().Be(-23);
        result.NewRp.Should().Be(977);
        result.NewRp.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CalculateRpAndRank_NegativeMidpoint_RoundsAwayFromZero()
    {
        var result = _service.CalculateRpAndRank(100, DomainMatchType.Ranked, position: 5, totalPlayers: 7);

        result.RpChange.Should().Be(-3);
        result.NewRp.Should().Be(97);
    }

    [Theory]
    [InlineData(DomainRankTier.Student, 0d)]
    [InlineData(DomainRankTier.Seeker, 0.25d)]
    [InlineData(DomainRankTier.Challenger, 0.5d)]
    [InlineData(DomainRankTier.Arcanist, 0.75d)]
    [InlineData(DomainRankTier.Expert, 1d)]
    [InlineData(DomainRankTier.Master, 1.25d)]
    [InlineData(DomainRankTier.GrandMaster, 1.5d)]
    [InlineData(DomainRankTier.Legend, 2d)]
    public void GetRankLossMultiplier_Tier_ReturnsExpectedMultiplier(DomainRankTier rankTier, double expectedMultiplier)
    {
        var result = _service.GetRankLossMultiplier(rankTier);

        result.Should().Be(expectedMultiplier);
    }

    [Fact]
    public void CalculateRpAndRank_StudentLastPlace_PreventsRpLoss()
    {
        var result = _service.CalculateRpAndRank(99, DomainMatchType.Ranked, position: 5, totalPlayers: 5);

        result.RpChange.Should().Be(0);
        result.NewRp.Should().Be(99);
        result.NewRankTier.Should().Be((int)DomainRankTier.Student);
    }

    [Fact]
    public void CalculateRpAndRank_ZeroRp_NeverReturnsNegativeRp()
    {
        var result = _service.CalculateRpAndRank(0, DomainMatchType.Ranked, position: 5, totalPlayers: 5);

        result.NewRp.Should().Be(0);
        result.RpChange.Should().Be(0);
    }

    [Theory]
    [InlineData(DomainMatchType.Friendly)]
    [InlineData(DomainMatchType.Private)]
    public void CalculateRpAndRank_NonRankedMatch_PreservesRpAndTier(DomainMatchType matchType)
    {
        var result = _service.CalculateRpAndRank(500, matchType, position: 0, totalPlayers: 0);

        result.RpChange.Should().Be(0);
        result.NewRp.Should().Be(500);
        result.OldRankTier.Should().Be((int)DomainRankTier.Challenger);
        result.NewRankTier.Should().Be((int)DomainRankTier.Challenger);
        result.MatchType.Should().Be((int)matchType);
        result.IsRanked.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, DomainRankTier.Student)]
    [InlineData(99, DomainRankTier.Student)]
    [InlineData(100, DomainRankTier.Seeker)]
    [InlineData(499, DomainRankTier.Seeker)]
    [InlineData(500, DomainRankTier.Challenger)]
    [InlineData(999, DomainRankTier.Challenger)]
    [InlineData(1_000, DomainRankTier.Arcanist)]
    [InlineData(1_999, DomainRankTier.Arcanist)]
    [InlineData(2_000, DomainRankTier.Expert)]
    [InlineData(3_499, DomainRankTier.Expert)]
    [InlineData(3_500, DomainRankTier.Master)]
    [InlineData(5_499, DomainRankTier.Master)]
    [InlineData(5_500, DomainRankTier.GrandMaster)]
    [InlineData(7_999, DomainRankTier.GrandMaster)]
    [InlineData(8_000, DomainRankTier.Legend)]
    [InlineData(10_000, DomainRankTier.Legend)]
    public void CalculateRankTier_Rp_ReturnsExpectedTier(int rp, DomainRankTier expectedTier)
    {
        var result = _service.CalculateRankTier(rp);

        result.Should().Be(expectedTier);
    }

    [Fact]
    public void CalculateRpAndRank_RankedGain_UpdatesStudentToSeeker()
    {
        var result = _service.CalculateRpAndRank(99, DomainMatchType.Ranked, position: 1, totalPlayers: 5);

        result.NewRp.Should().Be(129);
        result.OldRankTier.Should().Be((int)DomainRankTier.Student);
        result.NewRankTier.Should().Be((int)DomainRankTier.Seeker);
    }

    [Fact]
    public void CalculateRpAndRank_RankedGain_CrossesTierBoundary()
    {
        var result = _service.CalculateRpAndRank(490, DomainMatchType.Ranked, position: 1, totalPlayers: 5);

        result.NewRp.Should().Be(520);
        result.NewRankTier.Should().Be((int)DomainRankTier.Challenger);
    }

    [Fact]
    public void CalculateRpAndRank_HighRp_ReturnsLegendAndDoesNotAssignImmortal()
    {
        var result = _service.CalculateRpAndRank(8_000, DomainMatchType.Ranked, position: 3, totalPlayers: 5);

        result.NewRankTier.Should().Be((int)DomainRankTier.Legend);
        result.IsImmortal.Should().BeFalse();
        _service.IsImmortalEligible(10_000).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CalculateRpAndRank_InvalidTotalPlayers_ThrowsArgumentOutOfRange(int totalPlayers)
    {
        var act = () => _service.CalculateRpAndRank(100, DomainMatchType.Ranked, position: 1, totalPlayers);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CalculateRpAndRank_InvalidPosition_ThrowsArgumentOutOfRange(int position)
    {
        var act = () => _service.CalculateRpAndRank(100, DomainMatchType.Ranked, position, totalPlayers: 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateRpAndRank_NegativeOldRp_ThrowsArgumentOutOfRange()
    {
        var act = () => _service.CalculateRpAndRank(-1, DomainMatchType.Ranked, position: 1, totalPlayers: 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
