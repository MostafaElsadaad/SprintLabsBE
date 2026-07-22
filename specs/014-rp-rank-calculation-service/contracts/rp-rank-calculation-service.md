# Internal Contract: RP and Rank Calculation Service

This feature exposes no HTTP API or frontend contract. This document defines the internal service behavior for future match-completion code.

## Complete Calculation Contract

Input:
- `OldRp` as a non-negative integer.
- Existing Domain `MatchType`.
- `Position` as a one-based integer.
- `TotalPlayers` as an integer.

Output:
- `OldRp`
- `RpChange`
- `NewRp`
- `OldRankTier`
- `NewRankTier`
- `Position`
- `TotalPlayers`
- `MatchType`
- `IsRanked`
- `IsImmortal`

Rules:
- Ranked matches apply the ranked formula and validations.
- Friendly and Private matches return unchanged RP/tier and `IsRanked = false`.
- Shared result enum fields carry the integer values of the existing Domain enums.
- No database writes or external calls occur.

## Percentile Contract

Input:
- Position.
- Total players.

Output:
- Fractional percentile from 0 through 1.

Rules:
- Formula is `(TotalPlayers - Position) / (TotalPlayers - 1)`.
- Total players must be at least 2.
- Position must be between 1 and total players.

## BaseRP Contract

Input:
- Player percentile.

Output:
- BaseRP as a fractional number.

Rules:
- Formula is `(PlayerPercentile - 0.5) x 2 x 30`.
- First place produces 30, exact middle produces 0, and last place produces -30.

## Loss Contract

Input:
- Negative BaseRP.
- Old RP-derived rank tier.

Output:
- Rank-adjusted fractional RP change before rounding.

Rules:
- Student 0.
- Seeker 0.25.
- Challenger 0.5.
- Arcanist 0.75.
- Expert 1.0.
- Master 1.25.
- GrandMaster 1.5.
- Legend 2.0.
- Non-negative BaseRP is not multiplied.
- Final change is rounded to the nearest integer with midpoint values away from zero.

## Tier Contract

Input:
- Non-negative RP.

Output:
- Existing Domain RankTier from Student through Legend.

Rules:
- Boundaries are 0, 100, 500, 1000, 2000, 3500, 5500, and 8000.
- 8000 or more returns Legend.
- Immortal is never returned from RP alone.

## Immortal Hook Contract

Input:
- RP or calculated tier context sufficient for a future eligibility check.

Output:
- False for this feature.

Rules:
- No leaderboard query.
- No Top 10 calculation.
- No automatic Immortal assignment.
