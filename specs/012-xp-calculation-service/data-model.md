# Data Model: XP Calculation Service

This feature has no database schema changes and no persisted entities. The models below are calculation inputs/outputs only.

## Ordered Question Result

Represents one answered question in the order it occurred.

Fields:
- `IsCorrect`: boolean indicating whether the answer was correct.

Validation rules:
- Results must be processed in the caller-provided order.
- Empty collections are valid and produce `AnswerXp = 0`.
- Wrong answers produce 0 answer XP and reset the active streak to 0.

Relationships:
- None. Future match completion may map from `MatchQuestionResult`, but the calculator should not require EF entities.

## XP Calculation Result

Represents XP components produced by the calculator.

Fields:
- `AnswerXp`: XP from ordered answer outcomes.
- `MatchResultXp`: XP from winning or participating.
- `MissionXp`: XP from supported mission difficulty, or 0 when no mission applies.
- `TotalXp`: sum of answer XP, match result XP, and mission XP.

Validation rules:
- `TotalXp` must equal `AnswerXp + MatchResultXp + MissionXp`.
- Component values must not imply persistence, level changes, RP changes, rank changes, or mission completion.

Relationships:
- Maps conceptually to future `MatchRewardResult` XP fields.
- Does not update `PlayerProfile`, `MatchRewardResult`, `PlayerXpLog`, or any other persisted model.

## Mission Difficulty

Supported mission difficulty categories for XP-only mission rewards.

Values:
- `Normal`: 25 XP
- `Mid`: 50 XP
- `Hard`: 100 XP

Validation rules:
- Only supported enum values should be accepted.
- Mission XP does not produce RP or rank changes.

## Match Result Outcome

Represents whether a player won or participated without winning.

Fields:
- `IsWinner`: boolean.

Rules:
- Winner returns 50 XP.
- Non-winner participant returns 20 XP.
