# Tasks: RP and Rank Calculation Service

**Input**: Design documents from `specs/014-rp-rank-calculation-service/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/rp-rank-calculation-service.md, quickstart.md

**Tests**: Unit tests are required by the feature specification. Add tests before the corresponding implementation and confirm they fail for the expected missing behavior.

**Organization**: Tasks are grouped by user story so each RP/rank rule can be implemented and verified as a focused increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes a different file and does not depend on unfinished behavior
- **[Story]**: Maps the task to a user story from spec.md
- Every task includes an exact repository-relative file path

## Phase 1: Setup

**Purpose**: Confirm the existing progression-service conventions before adding files

- [X] T001 Inspect `Domain/Enums/ProgressionEnums.cs`, `Domain/Services/IXpCalculationService.cs`, `Domain/Services/ILevelProgressionService.cs`, `Infrastructure/Services/XpCalculationService.cs`, `Infrastructure/Services/LevelProgressionService.cs`, `Infrastructure/ServiceConfig.cs`, `Shared/Responses/XpCalculationResult.cs`, `Shared/Responses/LevelProgressionResult.cs`, and existing progression tests to confirm naming, dependency direction, DI lifetime, validation, and test style

---

## Phase 2: Foundational Contracts

**Purpose**: Add the cross-layer result and Domain service contract required by every user story

**CRITICAL**: Complete this phase before implementing any RP/rank behavior.

- [X] T002 [P] Create `Shared/Responses/RpRankCalculationResult.cs` with OldRp, RpChange, NewRp, integer OldRankTier/NewRankTier/MatchType values, Position, TotalPlayers, IsRanked, and IsImmortal
- [X] T003 [P] Create `Domain/Services/IRpRankCalculationService.cs` with typed Domain MatchType/RankTier methods for complete calculation, percentile, BaseRP, loss multiplier, tier derivation, and Immortal eligibility, returning `Shared.Responses.RpRankCalculationResult` where applicable

**Checkpoint**: Shared result and typed Domain service contract are ready without adding a Shared-to-Domain reference or duplicating enums.

---

## Phase 3: User Story 1 - Calculate Ranked Match RP (Priority: P1) MVP

**Goal**: Calculate ranked percentile and BaseRP, produce positive/negative rounded RP outcomes, enforce ranked input validation, and return a deterministic result.

**Independent Test**: First, exact-middle, and last positions produce percentiles 1, 0.5, and 0; BaseRP values 30, 0, and -30; first place gains RP; invalid ranked player counts/positions are rejected; repeated inputs return identical results.

### Tests for User Story 1

- [X] T004 [US1] Add failing tests for first/middle/last percentile values, BaseRP values, positive first-place RP, negative last-place RP for a loss-enabled tier, midpoint rounding away from zero, deterministic output, invalid ranked TotalPlayers, invalid ranked Position, and negative OldRp in `SprintLabs.Tests/Features/RpRankCalculationService/RpRankCalculationServiceTests.cs`

### Implementation for User Story 1

- [X] T005 [US1] Create `Infrastructure/Services/RpRankCalculationService.cs` and implement ranked input validation, percentile formula, BaseRP formula, positive/negative calculation flow, midpoint rounding away from zero, complete result composition, and deterministic behavior against `IRpRankCalculationService`

**Checkpoint**: Ranked formula helpers and complete ranked calculation are independently testable without EF Core, repositories, APIs, or persistence.

---

## Phase 4: User Story 2 - Apply Rank-Sensitive Loss Protection (Priority: P2)

**Goal**: Apply the exact Student-through-Legend loss multipliers and prevent losses from reducing RP below zero.

**Independent Test**: The same negative BaseRP uses each defined tier multiplier; Student loses no RP; capped losses return NewRp 0 and RpChange equal to NewRp minus OldRp.

### Tests for User Story 2

- [X] T006 [US2] Extend `SprintLabs.Tests/Features/RpRankCalculationService/RpRankCalculationServiceTests.cs` with failing theory tests for Student 0, Seeker 0.25, Challenger 0.5, Arcanist 0.75, Expert 1.0, Master 1.25, GrandMaster 1.5, and Legend 2.0 multipliers plus Student loss protection and zero-RP clamping/effective-delta behavior

### Implementation for User Story 2

- [X] T007 [US2] Implement exact old-tier loss multiplier selection, negative BaseRP adjustment, zero-RP clamping, and effective RpChange recomputation in `Infrastructure/Services/RpRankCalculationService.cs`

**Checkpoint**: Every rank-sensitive loss rule and RP floor is covered by focused unit tests.

---

## Phase 5: User Story 3 - Preserve RP Outside Ranked Matches (Priority: P3)

**Goal**: Ensure Friendly and Private matches never change RP or rank and do not apply ranked placement validation.

**Independent Test**: Friendly and Private inputs return RpChange 0, unchanged RP/tier values, IsRanked false, and succeed even when their position/player-count values would be invalid for Ranked.

### Tests for User Story 3

- [X] T008 [US3] Extend `SprintLabs.Tests/Features/RpRankCalculationService/RpRankCalculationServiceTests.cs` with failing tests for Friendly and Private unchanged RP/tier output, IsRanked false, and bypass of ranked placement validation

### Implementation for User Story 3

- [X] T009 [US3] Add the Friendly/Private early-return path with unchanged RP/tier values, IsRanked false, and no ranked position/player-count validation in `Infrastructure/Services/RpRankCalculationService.cs`

**Checkpoint**: Both non-ranked modes are safe to play without affecting competitive progression.

---

## Phase 6: User Story 4 - Update Rank Tier Without Assigning Immortal (Priority: P4)

**Goal**: Derive Student-through-Legend tiers from RP, support boundary crossings, and expose an Immortal hook that remains false.

**Independent Test**: Every RP boundary maps to the expected tier; ranked gains/losses cross tier boundaries correctly; 8000+ remains Legend; Immortal is never auto-assigned.

### Tests for User Story 4

- [X] T010 [US4] Extend `SprintLabs.Tests/Features/RpRankCalculationService/RpRankCalculationServiceTests.cs` with failing tests for all tier lower/upper boundaries, Student-to-Seeker and additional upward/downward boundary crossings, 8000+ Legend behavior, and false Immortal eligibility/result values

### Implementation for User Story 4

- [X] T011 [US4] Implement non-negative RP-to-tier mapping through Legend, old/new tier derivation, cross-boundary result values, and the false-only Immortal eligibility hook/result in `Infrastructure/Services/RpRankCalculationService.cs`

**Checkpoint**: Rank tiers update from RP while Immortal remains reserved for future leaderboard-aware logic.

---

## Phase 7: Integration and Verification

**Purpose**: Register the completed service and verify architecture, build, tests, and scope boundaries

- [X] T012 Register `IRpRankCalculationService` to `RpRankCalculationService` with scoped lifetime in `Infrastructure/ServiceConfig.cs`
- [X] T013 Review `Infrastructure/Infrastructure.csproj`, `Shared/Shared.csproj`, and the complete feature diff to confirm no Infrastructure-to-Application or Shared-to-Domain reference, duplicate enum, API, MediatR handler, EF Core/database write, migration, PlayerProfile update, rank-log/reward persistence, XP/level logic, leaderboard implementation, login change, authorization change, or unrelated refactor was introduced
- [X] T014 Run `dotnet build SprintLabs.sln` from the repository root and fix compile failures without expanding feature scope
- [X] T015 Run `dotnet test SprintLabs.sln --filter RpRankCalculationService` and fix focused RP/rank test failures
- [X] T016 Run `dotnet test SprintLabs.Tests\Compass.Tests.csproj` and fix regressions while preserving existing XP, level, B2B, and B2C behavior

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational Contracts (Phase 2)**: Depends on T001 and blocks service/test implementation.
- **User Story 1 (Phase 3)**: Depends on T002 and T003; establishes the calculation service and core ranked formula.
- **User Story 2 (Phase 4)**: Depends on T005; completes negative ranked outcomes and loss protection.
- **User Story 3 (Phase 5)**: Depends on T005 but can be implemented in parallel with User Story 2 if separate work is coordinated in the shared implementation/test files.
- **User Story 4 (Phase 6)**: Depends on T005 and integrates with T007 for loss-driven downward tier transitions.
- **Integration and Verification (Phase 7)**: Depends on all selected user-story phases.

### User Story Dependencies

- **User Story 1 (P1)**: First implementable calculation increment after foundational contracts; suggested MVP.
- **User Story 2 (P2)**: Builds on the ranked calculation path from US1.
- **User Story 3 (P3)**: Builds on the common result composition from US1 but is behaviorally independent from loss multipliers.
- **User Story 4 (P4)**: Uses the RP result from US1/US2 to verify tier transitions and adds the Immortal placeholder.

### Within Each User Story

- Add focused tests first and confirm the new cases fail for the intended missing behavior.
- Implement only the behavior required by that story in `RpRankCalculationService.cs`.
- Run the focused test filter at each checkpoint when practical.
- Keep result invariants intact: `RpChange = NewRp - OldRp`, `NewRp >= 0`, and `IsImmortal = false`.

### Parallel Opportunities

- T002 and T003 can run in parallel because they create separate foundational files.
- US2 and US3 can be reasoned about in parallel after US1, but edits must be coordinated because both touch the same service and test files.
- Architecture review in T013 can begin while DI registration is prepared, but final verification must inspect the completed diff.

---

## Parallel Example: Foundational Contracts

```text
Task T002: Create Shared/Responses/RpRankCalculationResult.cs
Task T003: Create Domain/Services/IRpRankCalculationService.cs
```

## Parallel Example: User Stories 2 and 3

```text
Developer A: Add and implement rank loss multiplier and RP-floor cases for US2.
Developer B: Prepare Friendly/Private preservation cases for US3, then coordinate changes to the shared service/test files.
```

---

## Implementation Strategy

### MVP First: User Story 1

1. Complete T001-T003.
2. Add US1 formula and ranked-calculation tests in T004.
3. Implement the core service in T005.
4. Run the focused test filter and validate formula/validation behavior.

### Incremental Delivery

1. Setup and contracts establish dependency-safe types.
2. US1 establishes ranked formula and result composition.
3. US2 completes rank-sensitive losses and zero-floor behavior.
4. US3 protects Friendly and Private matches.
5. US4 completes rank tiers and the Immortal placeholder.
6. Register, build, run focused tests, then run the full test project.

## Notes

- Reuse `MatchType` and `RankTier` from `Domain/Enums/ProgressionEnums.cs`; do not duplicate or relocate them.
- Shared result enum properties are integer values because Shared cannot reference Domain.
- Do not add a Shared request model unless the plan is intentionally revised.
- Do not create APIs, handlers, repositories, migrations, persistence logic, or leaderboard dependencies.
- Keep one public class per file and modify the fewest files possible.
