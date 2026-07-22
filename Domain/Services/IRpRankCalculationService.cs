using DomainMatchType = Domain.Enums.MatchType;
using DomainRankTier = Domain.Enums.RankTier;

using Shared.Responses;

namespace Domain.Services;

public interface IRpRankCalculationService
{
    RpRankCalculationResult CalculateRpAndRank(int oldRp, DomainMatchType matchType, int position, int totalPlayers);

    double CalculatePlayerPercentile(int position, int totalPlayers);

    double CalculateBaseRp(double playerPercentile);

    double GetRankLossMultiplier(DomainRankTier rankTier);

    DomainRankTier CalculateRankTier(int rp);

    bool IsImmortalEligible(int rp);
}
