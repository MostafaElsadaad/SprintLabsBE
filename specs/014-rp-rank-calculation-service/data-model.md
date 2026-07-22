# Data Model: RP and Rank Calculation Service

This feature has no database schema changes and no persisted entities. The model below is an internal calculation result only.

## RP/Rank Calculation Input

The Domain service method receives named parameters rather than a Shared request model.

Fields:
- `OldRp`: player's RP before this match.
- `MatchType`: existing Domain match type.
- `Position`: one-based finishing position.
- `TotalPlayers`: number of ranked participants.

Validation rules:
- `OldRp` must be non-negative for every match type.
- Ranked `TotalPlayers` must be at least 2.
- Ranked `Position` must be between 1 and `TotalPlayers`, inclusive.
- Friendly and Private results do not apply ranked placement validation.

Relationships:
- Future match completion may source these values from PlayerProfile, Match, and MatchPlayer.
- This feature does not read or update those entities.

## RP/Rank Calculation Result

Represents the complete calculated result returned to a future match-completion workflow.

Fields:
- `OldRp`: validated input RP.
- `RpChange`: effective applied integer change after rounding and zero-floor clamping.
- `NewRp`: `max(0, OldRp + rounded calculated change)`.
- `OldRankTier`: integer value of the existing Domain tier derived from OldRp.
- `NewRankTier`: integer value of the existing Domain tier derived from NewRp.
- `Position`: supplied position for result context.
- `TotalPlayers`: supplied player count for result context.
- `MatchType`: integer value of the existing Domain match type.
- `IsRanked`: true only when the ranked formula was applied.
- `IsImmortal`: false-only placeholder for future Top 10 qualification.

Validation and invariants:
- `NewRp` is never negative.
- `RpChange` always equals `NewRp - OldRp`.
- Friendly and Private results have `RpChange = 0`, `NewRp = OldRp`, and unchanged tiers.
- Old/new rank tier values correspond to existing `Domain.Enums.RankTier` numeric values.
- `IsImmortal` is false in every result for this feature.

Relationships:
- Maps conceptually to future MatchRewardResult and PlayerRankLog fields.
- Does not persist either record or update PlayerProfile.

## Rank Tier Mapping

| RP Range | Rank Tier | Loss Multiplier |
|----------|-----------|-----------------|
| 0-99 | Student | 0 |
| 100-499 | Seeker | 0.25 |
| 500-999 | Challenger | 0.5 |
| 1000-1999 | Arcanist | 0.75 |
| 2000-3499 | Expert | 1.0 |
| 3500-5499 | Master | 1.25 |
| 5500-7999 | GrandMaster | 1.5 |
| 8000+ | Legend | 2.0 |

Immortal has no RP range or loss multiplier in this feature. It remains dependent on future global Top 10 Legend data.

## Calculation State Transition

Ranked result:

`OldRp -> percentile -> BaseRP -> optional loss multiplier -> rounded change -> zero floor -> NewRp -> NewRankTier`

Friendly or Private result:

`OldRp -> unchanged NewRp and tier`
