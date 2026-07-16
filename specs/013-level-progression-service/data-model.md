# Data Model: Level Progression Service

This feature has no database schema changes and no persisted entities. The models below are calculation inputs/outputs only.

## Level Progression Request

Represents the input needed to calculate level progression.

Fields:
- `OldTotalXp`: total lifetime XP before the new reward.
- `XpGained`: XP gained from the current reward.

Validation rules:
- `OldTotalXp` must be non-negative.
- `XpGained` must be non-negative.
- Request must not require `PlayerProfile` or any persisted entity.

Relationships:
- Future match completion may create this request after XP calculation.
- Does not update PlayerProfile, MatchRewardResult, PlayerXpLog, or any database row.

## Level Progression Result

Represents the calculated level progression output.

Fields:
- `OldLevel`: level derived from `OldTotalXp`.
- `NewLevel`: level derived from `OldTotalXp + XpGained`.
- `OldTotalXp`: original total lifetime XP.
- `NewTotalXp`: total lifetime XP after applying gained XP.
- `XpGained`: XP included in this calculation.
- `LeveledUp`: true when `NewLevel > OldLevel`.
- `LevelsGained`: `NewLevel - OldLevel`, never less than 0.
- `XpRequiredForNextLevel`: XP required to advance from `NewLevel` to `NewLevel + 1`.
- `Unlocks`: placeholder list for future unlock results.

Validation rules:
- Levels must never be lower than 1.
- `NewTotalXp` must equal `OldTotalXp + XpGained`.
- `LevelsGained` must be 0 when no level changes.
- `Unlocks` must be empty for now.

Relationships:
- Maps conceptually to future `MatchRewardResult` level fields.
- Does not persist any progression changes.

## Level Unlock Result

Represents a placeholder future unlock hook result.

Fields:
- None required for this feature, or placeholder values only if implementation needs an object shape.

Validation rules:
- Must not create real unlocks.
- Must not imply entitlement, inventory, mission, shop, or content access.

## Level Requirement

Represents XP required for a level transition.

Fields:
- `Level`: current level used in the formula.
- `RequiredXp`: `ceil(150 * Level^1.25)`.

Validation rules:
- `Level` must be treated as at least 1.
- Required XP must use ceiling rounding.
