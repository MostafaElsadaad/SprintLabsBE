# Internal Contract: Level Progression Service

This feature exposes no HTTP API and no frontend contract. The contract below documents the expected internal service behavior for future match completion code.

## Service Capabilities

The level progression service must provide behavior equivalent to:

- Calculate XP required to advance from a level.
- Calculate old level from old total lifetime XP.
- Calculate new level from old total lifetime XP plus gained XP.
- Support multiple level-ups from one XP gain.
- Return old/new level and old/new total XP values.
- Return next-level XP requirement.
- Return a placeholder unlock hook result/list.

## Required XP Contract

Input:
- Level number.

Output:
- Integer XP requirement.

Rules:
- Formula is `150 x Level^1.25`.
- Use ceiling rounding.
- Level is treated as at least 1.

## Progression Contract

Input:
- `OldTotalXp`
- `XpGained`

Output:
- `OldLevel`
- `NewLevel`
- `OldTotalXp`
- `NewTotalXp`
- `XpGained`
- `LeveledUp`
- `LevelsGained`
- `XpRequiredForNextLevel`
- `Unlocks`

Rules:
- `NewTotalXp = OldTotalXp + XpGained`.
- XP gained must not be negative.
- Level must never be lower than 1.
- Multiple level-ups must be included.
- No database writes occur.

## Unlock Hook Contract

Input:
- `OldLevel`
- `NewLevel`

Output:
- Empty unlock result list for now.

Rules:
- No real unlock logic.
- No unlock persistence.
- No entitlement side effects.
