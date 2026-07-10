# Tasks: XP Calculation Service

**Input**: Design documents from `specs/012-xp-calculation-service/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/xp-calculator.md](./contracts/xp-calculator.md), [quickstart.md](./quickstart.md)

**Tests**: Unit tests are required by the feature specification.

**Organization**: Tasks are grouped by user story and ordered so each XP capability can be implemented and tested independently.

## Phase 1: Inspection and Setup

**Purpose**: Confirm existing SprintLabs patterns before adding the calculator.

- [x] T001 [P] Inspect existing service/helper patterns in `Domain/Services/`, `Infrastructure/Services/`, and `Application/ServiceConfig.cs`
- [x] T002 [P] Inspect existing unit test setup in `SprintLabs.Tests/Compass.Tests.csproj` and `SprintLabs.Tests/Features/`
- [x] T003 [P] Inspect existing enum conventions in `Domain/Enums/`
- [x] T004 Decide XP calculator location using `Domain/Services/` and `Application/ServiceConfig.cs`

---

## Phase 2: Foundational Calculator Shape

**Purpose**: Add the minimal reusable calculator surface used by all XP rules.

- [x] T005 Add mission difficulty enum or reuse existing one if present in `Domain/Enums/MissionDifficulty.cs`
- [x] T006 Add XP calculator service/helper in `Domain/Services/IXpCalculationService.cs`, `Domain/Services/XpCalculationService.cs`, `Domain/Services/XpQuestionResult.cs`, and `Domain/Services/XpCalculationResult.cs`

---

## Phase 3: User Story 1 - Calculate Answer XP From Ordered Results (Priority: P1) MVP

**Goal**: Calculate answer XP from ordered correct/wrong answer outcomes with streak multipliers and wrong-answer resets.

**Independent Test**: Pass ordered question outcomes to the calculator and verify answer XP totals for correct answers, wrong answers, streak thresholds, and reset behavior.

### Tests for User Story 1

- [x] T007 [US1] Add unit tests for correct answer XP in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`
- [x] T008 [US1] Add unit tests for wrong answer XP in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`
- [x] T009 [US1] Add unit tests for streak thresholds in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`
- [x] T010 [US1] Add unit tests for wrong answer resetting streak in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Implement correct answer XP in `Domain/Services/XpCalculationService.cs`
- [x] T012 [US1] Implement streak multiplier in `Domain/Services/XpCalculationService.cs`
- [x] T013 [US1] Implement wrong-answer streak reset in `Domain/Services/XpCalculationService.cs`

---

## Phase 4: User Story 2 - Calculate Match Result XP (Priority: P2)

**Goal**: Calculate winner and non-winner participant XP.

**Independent Test**: Call the match result XP calculation for winner and non-winner inputs and verify 50 XP and 20 XP.

### Tests for User Story 2

- [x] T014 [US2] Add unit tests for winner/participant XP in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`

### Implementation for User Story 2

- [x] T015 [US2] Implement winner/participant XP in `Domain/Services/XpCalculationService.cs`

---

## Phase 5: User Story 3 - Calculate Mission XP Separately (Priority: P3)

**Goal**: Calculate mission XP for Normal, Mid, and Hard difficulties.

**Independent Test**: Call the mission XP helper for each supported mission difficulty and verify 25, 50, and 100 XP.

### Tests for User Story 3

- [x] T016 [US3] Add unit tests for mission XP in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`

### Implementation for User Story 3

- [x] T017 [US3] Implement mission XP helper in `Domain/Services/XpCalculationService.cs`

---

## Phase 6: User Story 4 - Combine XP Components (Priority: P4)

**Goal**: Calculate total XP from answer XP, match result XP, and mission XP.

**Independent Test**: Pass component XP values to the calculator and verify total XP equals the sum.

### Tests for User Story 4

- [x] T018 [US4] Add unit tests for total XP in `SprintLabs.Tests/Features/XpCalculationService/XpCalculationServiceTests.cs`

### Implementation for User Story 4

- [x] T019 [US4] Implement total XP calculation in `Domain/Services/XpCalculationService.cs`

---

## Phase 7: Validation

**Purpose**: Verify the calculator builds and the relevant tests pass.

- [x] T020 Run `dotnet build SprintLabs.sln` from `K:\Projects\DotNet\SprintLabsbkp`
- [x] T021 Run relevant tests with `dotnet test SprintLabs.sln --filter XpCalculationService` from `K:\Projects\DotNet\SprintLabsbkp`

---

## Dependencies & Execution Order

- **Phase 1** must complete before adding calculator files.
- **Phase 2** must complete before user story implementation.
- **US1** is the MVP and should be completed first because answer XP is the core calculator behavior.
- **US2**, **US3**, and **US4** can follow after the calculator shape exists.
- **Validation** runs after all selected user stories are implemented.

## Parallel Opportunities

- T001, T002, and T003 can be performed in parallel.
- T007, T008, T009, and T010 are sequential in the checklist because they modify the same test file.
- T014, T016, and T018 are sequential in the checklist because they modify the same test file.

## Implementation Strategy

1. Complete inspection tasks and confirm the calculator location.
2. Add the minimal enum/service/helper surface.
3. Implement US1 first and validate answer XP behavior.
4. Add match result XP, mission XP, and total XP incrementally.
5. Run build and relevant tests.
