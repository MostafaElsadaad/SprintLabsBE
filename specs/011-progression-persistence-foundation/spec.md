# Feature Specification: Progression Persistence Foundation

**Feature Branch**: `011-progression-persistence-foundation`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Feature name: Progression Persistence Foundation. Add the database tables, fields, enums, and EF Core relationships needed for XP, level, RP, rank, logs, match results, question results, and match rewards in Sprint Labs. The backend needs persistence support for the upcoming match completion and progression system. This feature only covers schema/domain persistence foundation. It excludes complete match API, XP/level/RP/rank calculations, leaderboards, match history APIs, progression read APIs, game server integration, mission system, real analytics, reports, dashboards, Google login changes, and community authorization changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Persist Player Progression State (Priority: P1)

Sprint Labs needs every player profile to hold durable progression totals so future match completion can update XP, level, currency, ranking points, rank tier, total completed matches, and total wins without depending on transient game-session state.

**Why this priority**: Match rewards and ranking cannot be implemented safely until each player profile has the required progression state with predictable starting values.

**Independent Test**: Can be fully tested by creating or inspecting a player profile record and confirming all progression fields exist with safe defaults.

**Acceptance Scenarios**:

1. **Given** a new or existing player profile, **When** progression persistence is inspected, **Then** the profile supports total XP, level, gold, ranked points, current rank tier, highest rank tier, total matches, and total wins.
2. **Given** a player profile has not completed any matches, **When** its progression values are inspected, **Then** XP, gold, ranked points, total matches, and total wins default to zero, level defaults to one, and both rank tier fields default to Student.
3. **Given** future progression logic needs to query players by ranking or progression, **When** player profile persistence is inspected, **Then** ranking and progression fields are searchable enough for future leaderboard and history work.

---

### User Story 2 - Store Match and Participant Results (Priority: P2)

Sprint Labs needs durable match records and per-player match participation records so future match completion can capture who played, what kind of match occurred, which community context applied, and each participant's aggregate performance.

**Why this priority**: Match completion cannot be audited or rewarded without a stable record of the match and the participating player profiles.

**Independent Test**: Can be fully tested by creating match and match participant records for both a B2C match with no community and a B2B school match with a community.

**Acceptance Scenarios**:

1. **Given** a future match is created, **When** match persistence is used, **Then** the match can store a match code, optional room identifier, optional community context, match type, status, start time, end time, completion time, and creation time.
2. **Given** a B2C match, **When** the match and participant records are stored, **Then** community context can be absent.
3. **Given** a B2B school match, **When** the match and participant records are stored, **Then** community context can identify the school community.
4. **Given** the same player is added to the same match more than once, **When** persistence is validated, **Then** duplicate participant records for that match and player are prevented.

---

### User Story 3 - Store Question-Level and Reward Results (Priority: P3)

Sprint Labs needs durable question-level results and reward summaries so future progression logic can audit answer outcomes, timing, streaks, XP components, level changes, ranked point changes, and rank tier changes after a match.

**Why this priority**: Reward and progression calculations are intentionally out of scope, but their outputs need a reliable storage target before completion logic can be built.

**Independent Test**: Can be fully tested by storing question result and reward result records linked to a match and player profile, then confirming required answer, timing, XP, level, RP, and rank fields are present.

**Acceptance Scenarios**:

1. **Given** a match has answer results, **When** question result records are stored, **Then** each result can capture the match, player profile, optional question reference, question type, correctness, answer timing, question timing, and streak values.
2. **Given** a match has reward output for a player, **When** reward result records are stored, **Then** the record can capture answer XP, match result XP, mission XP, total XP, old and new level, old and new total XP, RP change, old and new RP, and old and new rank tier.
3. **Given** the same player receives rewards for the same match more than once, **When** persistence is validated, **Then** duplicate reward records for that match and player are prevented.

---

### User Story 4 - Preserve Progression Audit Logs (Priority: P4)

Sprint Labs needs append-only XP and rank change logs so future progression changes can be audited, replayed, or explained to players and school operators.

**Why this priority**: Logs provide traceability for future rewards, ranking, and support workflows without requiring those workflows to be implemented now.

**Independent Test**: Can be fully tested by storing XP and rank log records for both B2C and B2B contexts and confirming each can be linked to a player profile, source, optional community, and related match or source record.

**Acceptance Scenarios**:

1. **Given** a player receives XP from a match answer, match result, or mission, **When** an XP log is stored, **Then** the source type, optional source record, base XP, multiplier, final XP, player profile, optional community context, and creation time can be captured.
2. **Given** a player receives ranked point changes, **When** a rank log is stored, **Then** the match, old RP, RP change, new RP, old rank tier, new rank tier, finishing position, total players, player profile, optional community context, and creation time can be captured.
3. **Given** a B2C progression event, **When** an XP or rank log is stored, **Then** community context can be absent.
4. **Given** a B2B school progression event, **When** an XP or rank log is stored, **Then** community context can identify the school community.

### Edge Cases

- A B2C match has no community context, membership, or student license.
- A B2B school match has community context but should not depend on progression records creating or validating community membership.
- A match is created but never completed, so completion and end timestamps remain absent.
- A match is cancelled and should remain distinguishable from completed matches.
- A question result may not have a direct question reference, but must still preserve answer outcome and timing data.
- A reward result is written more than once for the same match and player.
- A match participant is written more than once for the same match and player.
- XP logs may reference different source types and may not always have a source record id.
- Rank logs must represent both rank increases and rank decreases.
- Existing player profiles already have some progression fields and must retain their current values where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Player profiles MUST support total XP as Experience with default value 0.
- **FR-002**: Player profiles MUST support current Level with default value 1.
- **FR-003**: Player profiles MUST support Gold with default value 0.
- **FR-004**: Player profiles MUST support ranked points as Rp with default value 0.
- **FR-005**: Player profiles MUST support current RankTier with default value Student.
- **FR-006**: Player profiles MUST support HighestRankTier with default value Student.
- **FR-007**: Player profiles MUST support TotalMatches with default value 0.
- **FR-008**: Player profiles MUST support TotalWins with default value 0.
- **FR-009**: The system MUST define supported match types: Ranked, Friendly, and Private.
- **FR-010**: The system MUST define supported match statuses: Created, Started, Completed, and Cancelled.
- **FR-011**: The system MUST define supported rank tiers: Student, Seeker, Challenger, Arcanist, Expert, Master, GrandMaster, Legend, and Immortal.
- **FR-012**: The system MUST define supported XP source types: MatchAnswer, MatchResult, and Mission.
- **FR-013**: The system MUST store Matches with match code, optional room identifier, optional community context, match type, status, start time, optional end time, optional completion time, and creation time.
- **FR-014**: The system MUST store MatchPlayers with match, player profile, optional community context, position, winner flag, correct answer count, wrong answer count, max streak, total answer time, total question time, and creation time.
- **FR-015**: The system MUST prevent duplicate MatchPlayers for the same match and player profile.
- **FR-016**: The system MUST store MatchQuestionResults with match, player profile, optional question reference, question type, correctness, answer time, question time, streak before answer, streak after answer, and creation time.
- **FR-017**: The system MUST store MatchRewardResults with match, player profile, optional community context, answer XP, match result XP, mission XP, total XP, old level, new level, old total XP, new total XP, RP change, old RP, new RP, old rank tier, new rank tier, and creation time.
- **FR-018**: The system MUST prevent duplicate MatchRewardResults for the same match and player profile.
- **FR-019**: The system MUST store PlayerXpLogs with player profile, optional community context, source type, optional source record, base XP, multiplier, final XP, and creation time.
- **FR-020**: The system MUST store PlayerRankLogs with player profile, optional community context, match, old RP, RP change, new RP, old rank tier, new rank tier, position, total players, and creation time.
- **FR-021**: Player profiles MUST be relatable to many match participant records, question result records, reward result records, XP logs, and rank logs.
- **FR-022**: Matches MUST be relatable to many match participant records, question result records, and reward result records.
- **FR-023**: Communities MUST be relatable to many matches, match participant records, reward result records, XP logs, and rank logs where community context exists.
- **FR-024**: Community context MUST be optional for match and progression records that support B2C activity.
- **FR-025**: Match and progression records MUST NOT require CommunityUser or StudentLicense records.
- **FR-026**: The persistence model MUST support efficient future queries for match completion, match history, ranking, leaderboards, and progression logs.
- **FR-027**: The feature MUST create the required persistence migration.
- **FR-028**: The feature MUST NOT add match completion APIs, progression read APIs, match history APIs, leaderboards, dashboards, reports, game server integration, mission behavior, real analytics, or reward calculation.
- **FR-029**: The feature MUST NOT change Google login behavior.
- **FR-030**: The feature MUST NOT change community authorization behavior.

### Key Entities *(include if feature involves data)*

- **PlayerProfile**: The player's game profile and progression summary. It stores XP, level, gold, ranked points, current rank, highest rank, total completed matches, and total wins.
- **Match**: A future game session record. It stores identity, room, optional community context, type, lifecycle status, and timing.
- **MatchPlayer**: A player's participation summary for one match. It stores player identity, optional community context, outcome, answer counts, streak, timing totals, and position.
- **MatchQuestionResult**: A player's result for one question within a match. It stores answer correctness, timing, question identity when available, question type, and streak changes.
- **MatchRewardResult**: A player's reward and progression summary for one match. It stores XP components, total XP, level changes, RP changes, and rank tier changes.
- **PlayerXpLog**: An audit log for XP awards. It stores source type, optional source id, XP inputs, final XP, player profile, optional community context, and creation time.
- **PlayerRankLog**: An audit log for ranked point and rank tier changes. It stores match, RP change, rank movement, finishing position, total players, player profile, optional community context, and creation time.
- **Community Context**: Optional school/community ownership for B2B match and progression records. It is absent for B2C records.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of player profile records can represent XP, level, gold, RP, rank tier, highest rank tier, total matches, and total wins with safe defaults.
- **SC-002**: 100% of required match, participant, question result, reward result, XP log, and rank log record types can be created and related to their parent records.
- **SC-003**: 100% of tested B2C match and progression records can be stored without community context.
- **SC-004**: 100% of tested B2B match and progression records can be stored with community context.
- **SC-005**: 100% of tested duplicate participant records for the same match and player are rejected.
- **SC-006**: 100% of tested duplicate reward records for the same match and player are rejected.
- **SC-007**: 100% of supported match type, match status, rank tier, and XP source values are available for future progression workflows.
- **SC-008**: The feature can be validated without adding or calling any match completion, reward calculation, leaderboard, report, dashboard, or analytics workflow.
- **SC-009**: Existing login and community authorization behavior remains unchanged in regression validation.

## Assumptions

- Existing player profile identity remains the anchor for progression records.
- Existing B2C support means match and progression records must allow absent community context.
- Existing B2B school support means match and progression records may include community context when a school community owns the activity.
- Match reward and progression calculation rules will be specified in later features.
- Match APIs, history APIs, read APIs, leaderboards, dashboards, reports, analytics, and game server integration are separate future features.
- Question references may be absent because some match question sources may not map directly to a stored question record.
- Persistence should be prepared for idempotent future match completion by preventing duplicate participant and reward records for the same match and player.
