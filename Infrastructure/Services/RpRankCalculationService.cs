using Domain.Services;

using Shared.Responses;

using DomainMatchType = Domain.Enums.MatchType;
using DomainRankTier = Domain.Enums.RankTier;

namespace Infrastructure.Services;

public class RpRankCalculationService : IRpRankCalculationService
{
    private const int MinimumRankedPlayers = 2;

    public RpRankCalculationResult CalculateRpAndRank(
        int oldRp,
        DomainMatchType matchType,
        int position,
        int totalPlayers)
    {
        if (oldRp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(oldRp), oldRp, "Old RP must not be negative.");
        }

        var oldRankTier = CalculateRankTier(oldRp);

        if (matchType is DomainMatchType.Friendly or DomainMatchType.Private)
        {
            return CreateResult(oldRp, 0, oldRp, oldRankTier, oldRankTier, matchType, position, totalPlayers, false);
        }

        if (matchType != DomainMatchType.Ranked)
        {
            throw new ArgumentOutOfRangeException(nameof(matchType), matchType, "Unsupported match type.");
        }

        var playerPercentile = CalculatePlayerPercentile(position, totalPlayers);
        var baseRp = CalculateBaseRp(playerPercentile);
        var finalRp = baseRp < 0 ? baseRp * GetRankLossMultiplier(oldRankTier) : baseRp;
        var roundedRpChange = (int)Math.Round(finalRp, MidpointRounding.AwayFromZero);
        var newRp = (int)Math.Clamp((long)oldRp + roundedRpChange, 0, int.MaxValue);
        var rpChange = newRp - oldRp;
        var newRankTier = CalculateRankTier(newRp);

        return CreateResult(oldRp, rpChange, newRp, oldRankTier, newRankTier, matchType, position, totalPlayers, true);
    }

    public double CalculatePlayerPercentile(int position, int totalPlayers)
    {
        if (totalPlayers < MinimumRankedPlayers)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPlayers), totalPlayers, "Ranked matches require at least two players.");
        }

        if (position < 1 || position > totalPlayers)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position must be within the ranked player count.");
        }

        return (double)(totalPlayers - position) / (totalPlayers - 1);
    }

    public double CalculateBaseRp(double playerPercentile)
    {
        if (double.IsNaN(playerPercentile) || double.IsInfinity(playerPercentile) || playerPercentile < 0 || playerPercentile > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(playerPercentile), playerPercentile, "Player percentile must be between 0 and 1.");
        }

        return (playerPercentile - 0.5) * 2 * 30;
    }

    public double GetRankLossMultiplier(DomainRankTier rankTier)
    {
        return rankTier switch
        {
            DomainRankTier.Student => 0,
            DomainRankTier.Seeker => 0.25,
            DomainRankTier.Challenger => 0.5,
            DomainRankTier.Arcanist => 0.75,
            DomainRankTier.Expert => 1,
            DomainRankTier.Master => 1.25,
            DomainRankTier.GrandMaster => 1.5,
            DomainRankTier.Legend => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(rankTier), rankTier, "Rank tier does not support RP loss calculation.")
        };
    }

    public DomainRankTier CalculateRankTier(int rp)
    {
        if (rp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rp), rp, "RP must not be negative.");
        }

        return rp switch
        {
            < 100 => DomainRankTier.Student,
            < 500 => DomainRankTier.Seeker,
            < 1_000 => DomainRankTier.Challenger,
            < 2_000 => DomainRankTier.Arcanist,
            < 3_500 => DomainRankTier.Expert,
            < 5_500 => DomainRankTier.Master,
            < 8_000 => DomainRankTier.GrandMaster,
            _ => DomainRankTier.Legend
        };
    }

    public bool IsImmortalEligible(int rp)
    {
        if (rp < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rp), rp, "RP must not be negative.");
        }

        return false;
    }

    private RpRankCalculationResult CreateResult(
        int oldRp,
        int rpChange,
        int newRp,
        DomainRankTier oldRankTier,
        DomainRankTier newRankTier,
        DomainMatchType matchType,
        int position,
        int totalPlayers,
        bool isRanked)
    {
        return new RpRankCalculationResult
        {
            OldRp = oldRp,
            RpChange = rpChange,
            NewRp = newRp,
            OldRankTier = (int)oldRankTier,
            NewRankTier = (int)newRankTier,
            Position = position,
            TotalPlayers = totalPlayers,
            MatchType = (int)matchType,
            IsRanked = isRanked,
            IsImmortal = IsImmortalEligible(newRp)
        };
    }
}
