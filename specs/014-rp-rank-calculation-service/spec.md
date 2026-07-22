# Feature Specification: RP and Rank Calculation Service

**Feature Branch**: `014-rp-rank-calculation-service`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "Feature name: RP and Rank Calculation Service. Implement backend RP and rank calculation for Sprint Labs ranked matches. Ranked matches calculate percentile, BaseRP, rank-sensitive losses, RP floors, and rank tier changes. Friendly and private matches do not change RP. Immortal remains a placeholder until global Top 10 leaderboard data is available. This feature includes calculation logic and unit tests only and excludes APIs, persistence, player-profile updates, leaderboards, XP/level calculation, login or authorization changes, and migrations."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Calculate Ranked Match RP (Priority: P1)

Future ranked-match completion needs one trusted calculation that rewards stronger placements and penalizes weaker placements according to the player's current rank.

**Why this priority**: Ranked RP calculation is the core behavior. Incorrect placement or loss calculations would make competitive progression unfair and inconsistent.

**Independent Test**: Can be fully tested with an old RP value, placement, and player count by verifying percentile, base RP, rank-sensitive loss, rounded change, and non-negative new RP.

**Acceptance Scenarios**:

1. **Given** a ranked match with at least two players, **When** the first-place result is calculated, **Then** the player receives a positive RP change.
2. **Given** a ranked match with at least two players, **When** the last-place result is calculated for a rank that permits losses, **Then** the player receives a negative RP change unless the zero-RP floor limits that loss.
3. **Given** first, middle, and last placements, **When** percentile is calculated, **Then** it follows `(TotalPlayers - Position) / (TotalPlayers - 1)`.
4. **Given** a calculated percentile, **When** base RP is calculated, **Then** it follows `(PlayerPercentile - 0.5) x 2 x 30`.
5. **Given** a final fractional RP change, **When** the result is produced, **Then** it is rounded to the nearest integer with midpoint values rounded away from zero.

---

### User Story 2 - Apply Rank-Sensitive Loss Protection (Priority: P2)

Players at lower ranks need stronger protection from losses, while higher-ranked players take progressively larger penalties for poor placements.

**Why this priority**: Loss multipliers are essential to the intended ranked progression curve and must be applied consistently after base RP becomes negative.

**Independent Test**: Can be fully tested by using the same negative base RP outcome for each rank tier and verifying the tier-specific multiplier and resulting applied RP loss.

**Acceptance Scenarios**:

1. **Given** a Student player has negative base RP, **When** RP is calculated, **Then** the loss multiplier is 0 and no RP is lost.
2. **Given** a Seeker, Challenger, Arcanist, Expert, Master, GrandMaster, or Legend player has negative base RP, **When** RP is calculated, **Then** the respective multiplier is 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, or 2.0.
3. **Given** a calculated loss exceeds the player's old RP, **When** RP is applied, **Then** new RP is 0 and the reported RP change equals the actual capped loss.
4. **Given** base RP is zero or positive, **When** RP is calculated, **Then** no rank loss multiplier is applied.

---

### User Story 3 - Preserve RP Outside Ranked Matches (Priority: P3)

Players in friendly or private matches need to keep their existing RP and rank because those modes are not competitive ranked play.

**Why this priority**: Non-ranked modes must be safe to play without affecting competitive progression.

**Independent Test**: Can be fully tested by calculating friendly and private results and verifying all RP and tier values remain unchanged.

**Acceptance Scenarios**:

1. **Given** a friendly match, **When** rank progression is calculated, **Then** RP change is 0, old and new RP are equal, and old and new rank tiers are equal.
2. **Given** a private match, **When** rank progression is calculated, **Then** RP change is 0, old and new RP are equal, and old and new rank tiers are equal.
3. **Given** a non-ranked match, **When** rank progression is calculated, **Then** the result identifies that ranked calculation was not applied.

---

### User Story 4 - Update Rank Tier Without Assigning Immortal (Priority: P4)

Future match completion needs the player's tier recalculated after RP changes while reserving Immortal for a later leaderboard-aware decision.

**Why this priority**: Tier transitions make RP meaningful, while premature Immortal assignment would violate the Top 10 requirement.

**Independent Test**: Can be fully tested with RP values around every tier boundary and with an Immortal eligibility hook that does not grant Immortal.

**Acceptance Scenarios**:

1. **Given** new RP crosses from 99 to at least 100, **When** rank is recalculated, **Then** the new tier is Seeker.
2. **Given** new RP crosses any defined tier boundary, **When** rank is recalculated, **Then** the new tier matches the new RP range.
3. **Given** new RP is 8000 or higher, **When** rank is recalculated without global Top 10 data, **Then** the new tier is Legend.
4. **Given** Immortal eligibility is checked, **When** global leaderboard qualification is unavailable, **Then** the placeholder reports that Immortal was not assigned.

### Edge Cases

- Ranked calculation rejects a player count below 2.
- Ranked calculation rejects a position below 1 or above the total player count.
- First place produces percentile 1 and base RP 30.
- Last place produces percentile 0 and base RP -30 before loss multipliers and the zero-RP floor.
- A placement at the exact center of an odd-sized match produces percentile 0.5 and base RP 0.
- Fractional positive and negative RP changes use nearest-integer rounding with midpoint values away from zero.
- Old RP below 0 is invalid because RP cannot be negative.
- A Student cannot lose RP because the Student loss multiplier is 0.
- A loss cannot reduce new RP below 0, and the returned RP change reflects only the amount actually applied.
- Friendly and private results preserve RP and tier without running ranked placement rules.
- Tier boundary values map exactly: 0 Student, 100 Seeker, 500 Challenger, 1000 Arcanist, 2000 Expert, 3500 Master, 5500 GrandMaster, and 8000 Legend.
- Immortal is never assigned from RP alone in this feature.
- The calculation does not save rank logs, match rewards, or player profile changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a reusable RP and rank calculation capability for future ranked-match completion workflows.
- **FR-002**: The calculation input MUST identify old RP, match type, position, and total players.
- **FR-003**: The calculation result MUST include OldRp, RpChange, NewRp, OldRankTier, NewRankTier, Position, TotalPlayers, MatchType, whether ranked calculation was applied, and an Immortal placeholder result.
- **FR-004**: The system MUST calculate RP changes only for Ranked matches.
- **FR-005**: For Friendly and Private matches, the system MUST return RpChange as 0, NewRp equal to OldRp, NewRankTier equal to OldRankTier, and ranked status as false.
- **FR-006**: Friendly and Private calculations MUST NOT require ranked placement or minimum-player validation because no ranked formula is applied.
- **FR-007**: For Ranked matches, TotalPlayers MUST be at least 2.
- **FR-008**: For Ranked matches, Position MUST be between 1 and TotalPlayers, inclusive.
- **FR-009**: OldRp MUST NOT be negative.
- **FR-010**: The system MUST calculate PlayerPercentile as `(TotalPlayers - Position) / (TotalPlayers - 1)` using fractional precision.
- **FR-011**: The system MUST calculate BaseRP as `(PlayerPercentile - 0.5) x 2 x 30`.
- **FR-012**: When BaseRP is zero or positive, the final RP value before rounding MUST equal BaseRP.
- **FR-013**: When BaseRP is negative, the final RP value before rounding MUST equal BaseRP multiplied by the loss multiplier for the player's old rank tier.
- **FR-014**: The system MUST use these loss multipliers: Student 0, Seeker 0.25, Challenger 0.5, Arcanist 0.75, Expert 1.0, Master 1.25, GrandMaster 1.5, and Legend 2.0.
- **FR-015**: The system MUST round the final RP change to the nearest integer, with midpoint values rounded away from zero.
- **FR-016**: NewRp MUST never be lower than 0.
- **FR-017**: When the zero-RP floor limits a loss, RpChange MUST equal `NewRp - OldRp` so the reported change is the amount actually applied.
- **FR-018**: The system MUST derive OldRankTier from OldRp using the defined RP tier ranges.
- **FR-019**: The system MUST derive NewRankTier from NewRp after applying the RP change and zero-RP floor.
- **FR-020**: Rank tiers MUST map as follows: Student 0-99, Seeker 100-499, Challenger 500-999, Arcanist 1000-1999, Expert 2000-3499, Master 3500-5499, GrandMaster 5500-7999, and Legend 8000 or higher.
- **FR-021**: The system MUST NOT auto-assign Immortal based on RP alone.
- **FR-022**: The system MUST expose a placeholder hook or result for future Immortal Top 10 eligibility logic.
- **FR-023**: Until global leaderboard qualification exists, the Immortal placeholder MUST report that Immortal was not assigned and 8000 or more RP MUST return Legend.
- **FR-024**: The system MUST calculate the same output for identical inputs.
- **FR-025**: Unit tests MUST cover first, middle, and last percentile values; base RP; positive and negative ranked outcomes; every loss multiplier; zero-RP clamping; Friendly and Private preservation; tier boundaries; Legend at 8000 or more RP; the Immortal placeholder; and invalid ranked inputs.
- **FR-026**: The feature MUST NOT add APIs, controllers, match completion endpoints, match history endpoints, or progression endpoints.
- **FR-027**: The feature MUST NOT save PlayerRankLogs, MatchRewardResults, leaderboard data, or PlayerProfile changes.
- **FR-028**: The feature MUST NOT calculate XP, levels, match history, or real Immortal Top 10 eligibility.
- **FR-029**: The feature MUST NOT change Google login behavior or community authorization behavior.
- **FR-030**: The feature MUST NOT add or modify database schema or migrations.

### Key Entities *(include if feature involves data)*

- **RP Calculation Input**: The old RP, match type, placement, and player count needed to evaluate one player's ranked result.
- **RP Calculation Result**: The old and new RP/tier state, effective RP change, placement context, ranked status, and Immortal placeholder outcome.
- **Rank Tier**: A named competitive band derived from RP. Student through Legend are RP-based; Immortal requires future global Top 10 qualification.
- **Immortal Eligibility Result**: A placeholder outcome indicating that no Immortal assignment was made in this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested first, middle, and last placements return the expected percentile and base RP values.
- **SC-002**: 100% of tested negative ranked outcomes apply the exact multiplier defined for the player's old rank tier.
- **SC-003**: 100% of tested RP results are whole numbers rounded to the nearest integer with midpoint values away from zero.
- **SC-004**: 100% of tested losses return NewRp at or above 0 and an RpChange consistent with the applied old-to-new RP difference.
- **SC-005**: 100% of tested Friendly and Private matches preserve RP and rank tier and report that ranked calculation was not applied.
- **SC-006**: 100% of tested RP boundary values map to the expected Student-through-Legend tier.
- **SC-007**: 100% of tested values at or above 8000 RP return Legend and never auto-assign Immortal.
- **SC-008**: 100% of invalid ranked player-count and position scenarios are rejected without producing a ranked result.
- **SC-009**: The feature can be validated without APIs, database writes, migrations, leaderboard persistence, XP/level calculation, login changes, or authorization changes.

## Assumptions

- Old rank tier is derived from OldRp so the input cannot contain an RP/tier mismatch.
- RpChange represents the effective applied change after enforcing the zero-RP floor; it therefore always equals NewRp minus OldRp.
- Friendly and Private matches bypass ranked placement validation because their position and player count do not affect RP.
- Existing players marked Immortal by a future leaderboard system are outside this calculation-only feature; this service derives RP-based tiers only through Legend.
- The future match completion workflow will decide when and how to persist RP, rank tiers, rank logs, and reward results.
- The existing MatchType and RankTier concepts are available from the progression persistence foundation.
