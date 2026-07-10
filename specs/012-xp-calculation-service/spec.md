# Feature Specification: XP Calculation Service

**Feature Branch**: `012-xp-calculation-service`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description: "Feature name: XP Calculation Service. Implement backend XP calculation for Sprint Labs match rewards. Backend should calculate XP from correct answers, streaks, match result, and missions so future match completion logic can use one trusted XP calculator. This feature only covers XP calculation logic and unit tests. It includes correct answer XP, streak multiplier, wrong answer breaking streak, winner/participant XP, mission XP helper, and unit tests. It excludes complete match API, saving match results, saving MatchRewardResults, saving PlayerXpLogs, updating PlayerProfile Experience or Level, RP calculation, rank tier calculation, leaderboards, match history APIs, mission persistence, real mission completion system, Google login changes, and community authorization changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Calculate Answer XP From Ordered Results (Priority: P1)

Future match completion needs a trusted way to calculate answer XP from the ordered sequence of question outcomes, including streak multipliers and streak resets after wrong answers.

**Why this priority**: Answer XP is the largest and most detailed part of match rewards. It must be deterministic before match completion can safely award player progression.

**Independent Test**: Can be fully tested by passing ordered correct/wrong answer outcomes to the calculator and verifying the answer XP total for each streak threshold and reset case.

**Acceptance Scenarios**:

1. **Given** a single correct answer and no active streak multiplier, **When** answer XP is calculated, **Then** the answer gives 10 XP.
2. **Given** a wrong answer, **When** answer XP is calculated, **Then** the answer gives 0 XP and the current streak is reset.
3. **Given** two correct answers in a row, **When** answer XP is calculated, **Then** the second correct answer uses a 1.2 multiplier.
4. **Given** four correct answers in a row, **When** answer XP is calculated, **Then** the fourth correct answer uses a 1.4 multiplier.
5. **Given** six correct answers in a row, **When** answer XP is calculated, **Then** the sixth correct answer uses a 1.8 multiplier.
6. **Given** eight correct answers in a row, **When** answer XP is calculated, **Then** the eighth correct answer uses a 2.2 multiplier.
7. **Given** ten or more correct answers in a row, **When** answer XP is calculated, **Then** correct answers at streak ten and above use a 3.0 multiplier.

---

### User Story 2 - Calculate Match Result XP (Priority: P2)

Future match completion needs a consistent reward for match participation and winning so every player receives the correct match result XP.

**Why this priority**: Match result XP is independent from answer XP and must be reusable for any future match completion flow.

**Independent Test**: Can be fully tested by calculating match result XP for a winner and a non-winner participant.

**Acceptance Scenarios**:

1. **Given** a winning participant, **When** match result XP is calculated, **Then** the result is 50 XP.
2. **Given** a non-winning participant, **When** match result XP is calculated, **Then** the result is 20 XP.

---

### User Story 3 - Calculate Mission XP Separately (Priority: P3)

Future mission systems need a simple XP helper that returns mission XP by difficulty without adding mission persistence or mission completion tracking.

**Why this priority**: Mission XP contributes to total XP but must remain isolated from match reward and RP/rank behavior.

**Independent Test**: Can be fully tested by calculating XP for Normal, Mid, and Hard mission difficulties.

**Acceptance Scenarios**:

1. **Given** a Normal mission difficulty, **When** mission XP is calculated, **Then** the result is 25 XP.
2. **Given** a Mid mission difficulty, **When** mission XP is calculated, **Then** the result is 50 XP.
3. **Given** a Hard mission difficulty, **When** mission XP is calculated, **Then** the result is 100 XP.
4. **Given** mission XP is calculated, **When** the result is used by future workflows, **Then** it contributes XP only and does not imply RP or rank changes.

---

### User Story 4 - Combine XP Components (Priority: P4)

Future match completion needs one trusted total XP value composed from answer XP, match result XP, and mission XP.

**Why this priority**: Total XP must be predictable before later features persist rewards or update player progression.

**Independent Test**: Can be fully tested by passing answer XP, match result XP, and mission XP values into the total calculation and verifying the sum.

**Acceptance Scenarios**:

1. **Given** answer XP, match result XP, and mission XP, **When** total XP is calculated, **Then** total XP equals the sum of all three components.
2. **Given** no mission XP applies, **When** total XP is calculated, **Then** total XP still equals answer XP plus match result XP.

### Edge Cases

- Ordered question results are empty, so answer XP should be 0.
- All answers are wrong, so answer XP should be 0 and no multiplier should apply.
- A wrong answer appears between correct answers, so the later correct answer starts a new streak.
- A streak falls between thresholds, so the latest reached threshold applies.
- A streak exceeds ten, so the 3.0 multiplier continues to apply.
- Mission XP is requested for only the supported mission difficulties.
- Mission XP must not create mission records or imply mission completion.
- XP calculation must not update player profile totals or persist logs.
- XP calculation must not calculate RP, rank tier, level, or leaderboard placement.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a reusable XP calculation capability for future match completion workflows.
- **FR-002**: The system MUST calculate 10 base XP for each correct answer before streak multiplier is applied.
- **FR-003**: The system MUST calculate 0 answer XP for wrong answers.
- **FR-004**: The system MUST calculate answer XP from ordered question results rather than relying only on aggregate max streak values.
- **FR-005**: The system MUST reset the active streak after every wrong answer.
- **FR-006**: The system MUST apply a 1.0 multiplier for streak 1.
- **FR-007**: The system MUST apply a 1.2 multiplier for streak 2 or 3.
- **FR-008**: The system MUST apply a 1.4 multiplier for streak 4 or 5.
- **FR-009**: The system MUST apply a 1.8 multiplier for streak 6 or 7.
- **FR-010**: The system MUST apply a 2.2 multiplier for streak 8 or 9.
- **FR-011**: The system MUST apply a 3.0 multiplier for streak 10 or greater.
- **FR-012**: The system MUST calculate 50 match result XP for a winner.
- **FR-013**: The system MUST calculate 20 match result XP for a non-winner participant.
- **FR-014**: The system MUST calculate 25 XP for Normal mission difficulty.
- **FR-015**: The system MUST calculate 50 XP for Mid mission difficulty.
- **FR-016**: The system MUST calculate 100 XP for Hard mission difficulty.
- **FR-017**: The system MUST keep mission XP isolated so mission XP does not produce RP or rank changes.
- **FR-018**: The system MUST calculate total XP as AnswerXp + MatchResultXp + MissionXp.
- **FR-019**: The system MUST expose answer XP, match result XP, mission XP, and total XP as separate calculated values so future reward persistence can store them independently.
- **FR-020**: The system MUST include unit tests covering correct answer XP, wrong answer XP, all listed streak thresholds, wrong-answer streak reset, winner XP, participant XP, all mission difficulties, and total XP sum.
- **FR-021**: The feature MUST NOT add APIs, controllers, match completion endpoints, or progression read endpoints.
- **FR-022**: The feature MUST NOT save match results, reward results, XP logs, mission records, or player profile progression fields.
- **FR-023**: The feature MUST NOT calculate RP, rank tier, level changes, leaderboards, or match history.
- **FR-024**: The feature MUST NOT change Google login behavior or community authorization behavior.

### Key Entities *(include if feature involves data)*

- **Ordered Question Result**: A minimal answer outcome used by the calculator. It identifies whether each answered question was correct and preserves the order needed for streak calculation.
- **XP Calculation Result**: The calculated reward output for future match completion, including AnswerXp, MatchResultXp, MissionXp, and TotalXp.
- **Mission Difficulty**: The supported mission difficulty category for mission XP: Normal, Mid, or Hard.
- **Match Result Outcome**: A winner/non-winner participant indicator used to calculate match result XP.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested correct-answer scenarios return the expected answer XP.
- **SC-002**: 100% of tested wrong-answer scenarios return 0 answer XP for wrong answers and reset streaks.
- **SC-003**: 100% of tested streak thresholds return the expected multiplier-adjusted XP.
- **SC-004**: 100% of tested winner and non-winner scenarios return 50 XP and 20 XP respectively.
- **SC-005**: 100% of tested Normal, Mid, and Hard mission scenarios return 25, 50, and 100 XP respectively.
- **SC-006**: 100% of tested total XP scenarios equal AnswerXp + MatchResultXp + MissionXp.
- **SC-007**: The feature can be validated without creating API endpoints, writing database records, changing login behavior, or changing community authorization.
- **SC-008**: Existing tested backend behavior continues to build and test successfully after the XP calculator is added.

## Assumptions

- Ordered question results are available to future match completion logic before reward persistence occurs.
- XP values should be whole numbers after multiplier application.
- Mission difficulty names are Normal, Mid, and Hard for this feature.
- Mission XP is optional in total XP and can be zero when no mission applies.
- Future features will decide when to persist MatchRewardResults, PlayerXpLogs, Experience, Level, RP, and RankTier.
- This feature is calculation-only and has no database or API contract.
