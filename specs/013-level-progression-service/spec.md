# Feature Specification: Level Progression Service

**Feature Branch**: `013-level-progression-service`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "Feature name: Level Progression Service. Implement backend level progression calculation for Sprint Labs player progression. Backend should calculate player level updates from total XP using the Sprint Labs level formula, support multiple level-ups from one XP reward, return old/new level, and expose a future unlock hook. This feature only covers level progression calculation logic and unit tests. It excludes complete match API, saving match results, saving MatchRewardResults, saving PlayerXpLogs, updating PlayerProfile in database, XP calculation from answers/streaks/missions, RP calculation, rank tier calculation, leaderboards, match history APIs, real unlock system, Google login changes, community authorization changes, and new migrations."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Calculate Level From XP Formula (Priority: P1)

Future match completion needs a trusted way to determine a player's level from total lifetime XP using the Sprint Labs level requirement formula.

**Why this priority**: Level calculation is the core progression rule. Future reward persistence must be able to trust the level result before updating any player profile.

**Independent Test**: Can be fully tested by requesting XP required for specific levels and by calculating level state from old total XP plus gained XP.

**Acceptance Scenarios**:

1. **Given** level 1, **When** required XP is calculated, **Then** the result uses `150 x 1^1.25` and returns the ceiling value.
2. **Given** a level where the formula produces a fractional value, **When** required XP is calculated, **Then** the required XP is rounded up.
3. **Given** a player has no XP history, **When** level progression is calculated, **Then** the minimum level is 1.
4. **Given** gained XP is 0, **When** level progression is calculated, **Then** the player level does not decrease.

---

### User Story 2 - Support Multiple Level-Ups (Priority: P2)

Future reward flows may grant enough XP for a player to gain more than one level from a single reward, and the backend must return the correct old and new progression state.

**Why this priority**: Match rewards can be large enough to cross multiple level thresholds. Incorrect multi-level behavior would make persisted progression inconsistent.

**Independent Test**: Can be fully tested by passing old total XP and a large gained XP amount, then verifying old level, new level, old total XP, new total XP, levels gained, and leveled-up state.

**Acceptance Scenarios**:

1. **Given** a player gains enough XP for one level, **When** progression is calculated, **Then** `NewLevel` is exactly one higher than `OldLevel`.
2. **Given** a player gains enough XP for multiple levels, **When** progression is calculated, **Then** all crossed level thresholds are reflected in `NewLevel` and `LevelsGained`.
3. **Given** old total XP and gained XP, **When** progression is calculated, **Then** `OldTotalXp`, `NewTotalXp`, and `XpGained` are returned.
4. **Given** the level increases, **When** progression is calculated, **Then** `LeveledUp` is true.
5. **Given** the level does not increase, **When** progression is calculated, **Then** `LeveledUp` is false and `LevelsGained` is 0.

---

### User Story 3 - Return Next Level Requirement (Priority: P3)

Future reward and profile experiences need to know how much XP is required for the next level after the progression result is calculated.

**Why this priority**: Next-level requirement is needed for future progress bars and reward summaries, but it depends on the same level formula.

**Independent Test**: Can be fully tested by calculating progression and verifying the returned next-level XP requirement for the resulting level.

**Acceptance Scenarios**:

1. **Given** a calculated new level, **When** progression is returned, **Then** it includes the XP requirement for the next level.
2. **Given** the level formula changes in only one place in the future, **When** next-level requirement is requested, **Then** it stays consistent with level calculation rules.

---

### User Story 4 - Provide Future Unlock Hook (Priority: P4)

Future progression features need an extension point for unlocks that may happen when a player levels up, without implementing the real unlock system now.

**Why this priority**: Unlocks are expected later, and a simple placeholder prevents future match completion from needing to reshape the level progression result.

**Independent Test**: Can be fully tested by calculating a level-up and verifying the unlock hook returns an empty list or placeholder result without failing.

**Acceptance Scenarios**:

1. **Given** a player levels up, **When** progression is calculated, **Then** the result includes a future unlock hook result/list.
2. **Given** unlock logic is not implemented yet, **When** the unlock hook is called, **Then** it returns an empty list or placeholder result without creating real unlocks.

### Edge Cases

- Old total XP is 0, so old level should be 1.
- Gained XP is 0, so new total XP equals old total XP and level should not decrease.
- Gained XP crosses exactly one level threshold.
- Gained XP crosses multiple level thresholds.
- Old total XP already maps to a level higher than 1.
- Required XP formula produces a fractional value, so ceiling must be used.
- Inputs should never produce a level lower than 1.
- Unlock hook must not create unlock records or imply real unlock entitlement.
- Level progression must not update player profiles, save logs, or persist match rewards.
- Level progression must not calculate XP from answers, RP, rank tier, leaderboards, or match history.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a reusable level progression calculation capability for future match completion workflows.
- **FR-002**: The system MUST calculate required XP for a level as `150 x Level^1.25`.
- **FR-003**: The system MUST use ceiling when required XP calculation produces a fractional value.
- **FR-004**: The system MUST treat player level as starting at 1.
- **FR-005**: The system MUST treat experience as total lifetime XP.
- **FR-006**: The system MUST calculate new total XP as old total XP plus gained XP.
- **FR-007**: The system MUST calculate the new level that corresponds to the new total XP.
- **FR-008**: The system MUST support multiple level-ups from one XP reward.
- **FR-009**: The system MUST never return a level lower than 1.
- **FR-010**: The system MUST return OldLevel.
- **FR-011**: The system MUST return NewLevel.
- **FR-012**: The system MUST return OldTotalXp.
- **FR-013**: The system MUST return NewTotalXp.
- **FR-014**: The system MUST return XpGained.
- **FR-015**: The system MUST return whether the player leveled up.
- **FR-016**: The system MUST return LevelsGained.
- **FR-017**: The system MUST return the XP required for the next level after progression is calculated.
- **FR-018**: The system MUST expose a future unlock hook result/list.
- **FR-019**: The future unlock hook MUST return an empty list or placeholder result and MUST NOT implement real unlock logic.
- **FR-020**: The system MUST include unit tests covering the XP formula, ceiling behavior, level 1 start, no-XP-gain behavior, one-level gain, multiple-level gain, old/new level, old/new total XP, levels gained, leveled-up true/false states, next-level XP requirement, and future unlock hook.
- **FR-021**: The feature MUST NOT add APIs, controllers, match completion endpoints, or progression read endpoints.
- **FR-022**: The feature MUST NOT save match results, reward results, XP logs, unlock records, or player profile progression fields.
- **FR-023**: The feature MUST NOT calculate XP from answers, streaks, missions, RP, rank tier, leaderboards, or match history.
- **FR-024**: The feature MUST NOT change Google login behavior or community authorization behavior.
- **FR-025**: The feature MUST NOT add a migration.

### Key Entities *(include if feature involves data)*

- **Level Progression Input**: The values needed to calculate progression, including old total XP and gained XP.
- **Level Progression Result**: The calculated output containing OldLevel, NewLevel, OldTotalXp, NewTotalXp, XpGained, LeveledUp, LevelsGained, next-level XP requirement, and unlock hook result/list.
- **Level Requirement**: The XP required for a level, calculated using the Sprint Labs formula and ceiling rounding.
- **Unlock Hook Result**: A placeholder list/result for future unlocks. It does not represent real unlock persistence or entitlement in this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested level requirement scenarios return the formula result with ceiling rounding.
- **SC-002**: 100% of tested progression scenarios never return a level lower than 1.
- **SC-003**: 100% of tested no-XP-gain scenarios keep the same level and report no level-up.
- **SC-004**: 100% of tested one-level and multiple-level gain scenarios return the expected new level and levels gained.
- **SC-005**: 100% of tested progression scenarios return accurate old/new total XP and gained XP values.
- **SC-006**: 100% of tested next-level requirement scenarios return a value consistent with the level formula.
- **SC-007**: 100% of tested unlock hook scenarios return an empty list or placeholder without failing.
- **SC-008**: The feature can be validated without API endpoints, database writes, migrations, login changes, authorization changes, or real unlock logic.

## Assumptions

- Old total XP and gained XP are non-negative inputs for this feature.
- Future features will decide when to persist PlayerProfile Experience and Level.
- Future features will decide how real unlocks are represented and persisted.
- This feature is calculation-only and has no database or API contract.
- Level thresholds are derived from total lifetime XP, not from XP gained in only the current match.
