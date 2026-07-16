# Tasks: Level Progression Service

**Input**: Design documents from `specs/013-level-progression-service/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/level-progression-service.md](./contracts/level-progression-service.md), [quickstart.md](./quickstart.md)

**Tests**: Unit tests are required by the feature specification.

**Organization**: Tasks are grouped by user story and ordered so each level progression capability can be implemented and tested independently.

## Phase 1: Inspection and Setup

**Purpose**: Confirm existing SprintLabs layering and test patterns before adding the service.

- [x] T001 [P] Inspect existing service layering pattern in `Domain/Services/`, `Infrastructure/Services/`, `Shared/`, and `Infrastructure/ServiceConfig.cs`
- [x] T002 [P] Inspect XP calculation service placement in `Domain/Services/IXpCalculationService.cs`, `Infrastructure/Services/XpCalculationService.cs`, `Shared/Requests/XpQuestionResult.cs`, and `Shared/Responses/XpCalculationResult.cs`
- [x] T003 [P] Inspect Shared result model conventions in `Shared/Requests/` and `Shared/Responses/`
- [x] T004 Inspect Infrastructure DI registration in `Infrastructure/ServiceConfig.cs`
- [x] T005 [P] Inspect unit test setup in `SprintLabs.Tests/Compass.Tests.csproj` and `SprintLabs.Tests/Features/`

---

## Phase 2: Foundational Service Shape

**Purpose**: Add the minimal reusable level progression service surface used by all level rules.

- [x] T006 Add level progression result model in `Shared/Responses/LevelProgressionResult.cs`
- [x] T007 Add future unlock placeholder model/list support in `Shared/Responses/LevelUnlockResult.cs`
- [x] T008 Add level progression request model in `Shared/Requests/LevelProgressionRequest.cs`
- [x] T009 Add level progression service interface in `Domain/Services/ILevelProgressionService.cs`
- [x] T010 Add level progression service implementation shell in `Infrastructure/Services/LevelProgressionService.cs`
- [x] T011 Register service in `Infrastructure/ServiceConfig.cs`

---

## Phase 3: User Story 1 - Calculate Level From XP Formula (Priority: P1) MVP

**Goal**: Calculate XP required for a level, use ceiling rounding, and keep the minimum level at 1.

**Independent Test**: Call the service for level requirement and no-XP progression scenarios and verify formula, ceiling, and level 1 behavior.

### Tests for User Story 1

- [x] T012 [US1] Add unit tests for formula in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`
- [x] T013 [US1] Add unit tests for no level-up in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`

### Implementation for User Story 1

- [x] T014 [US1] Implement XP required formula in `Infrastructure/Services/LevelProgressionService.cs`
- [x] T015 [US1] Implement ceiling rounding in `Infrastructure/Services/LevelProgressionService.cs`
- [x] T016 [US1] Implement total XP update calculation in `Infrastructure/Services/LevelProgressionService.cs`

---

## Phase 4: User Story 2 - Support Multiple Level-Ups (Priority: P2)

**Goal**: Calculate old/new level state from old total XP plus gained XP, including one or multiple level-ups.

**Independent Test**: Pass old total XP and gained XP values and verify old/new levels, totals, levels gained, and leveled-up state.

### Tests for User Story 2

- [x] T017 [US2] Add unit tests for one level-up in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`
- [x] T018 [US2] Add unit tests for multiple level-ups in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`
- [x] T019 [US2] Add unit tests for old/new values in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`

### Implementation for User Story 2

- [x] T020 [US2] Implement multiple level-ups in `Infrastructure/Services/LevelProgressionService.cs`
- [x] T021 [US2] Return old/new level in `Shared/Responses/LevelProgressionResult.cs` and `Infrastructure/Services/LevelProgressionService.cs`
- [x] T022 [US2] Return old/new total XP in `Shared/Responses/LevelProgressionResult.cs` and `Infrastructure/Services/LevelProgressionService.cs`
- [x] T023 [US2] Return levels gained in `Shared/Responses/LevelProgressionResult.cs` and `Infrastructure/Services/LevelProgressionService.cs`

---

## Phase 5: User Story 3 - Return Next Level Requirement (Priority: P3)

**Goal**: Include the XP requirement for the next level in the progression result.

**Independent Test**: Calculate progression and verify next-level XP requirement matches the same formula.

### Implementation for User Story 3

- [x] T024 [US3] Return next level XP requirement in `Shared/Responses/LevelProgressionResult.cs` and `Infrastructure/Services/LevelProgressionService.cs`

---

## Phase 6: User Story 4 - Provide Future Unlock Hook (Priority: P4)

**Goal**: Expose a future unlock hook that returns an empty placeholder result/list without real unlock logic.

**Independent Test**: Calculate a level-up and verify unlock hook output is present and empty.

### Tests for User Story 4

- [x] T025 [US4] Add unit tests for future unlock hook in `SprintLabs.Tests/Features/LevelProgressionService/LevelProgressionServiceTests.cs`

### Implementation for User Story 4

- [x] T026 [US4] Add future unlock hook in `Domain/Services/ILevelProgressionService.cs`, `Infrastructure/Services/LevelProgressionService.cs`, and `Shared/Responses/LevelProgressionResult.cs`

---

## Phase 7: Validation

**Purpose**: Verify the service builds and the relevant tests pass.

- [x] T027 Run `dotnet build SprintLabs.sln` from `K:\Projects\DotNet\SprintLabsbkp`
- [x] T028 Run relevant tests with `dotnet test SprintLabs.sln --filter LevelProgressionService` from `K:\Projects\DotNet\SprintLabsbkp`

---

## Dependencies & Execution Order

- **Phase 1** must complete before adding level progression files.
- **Phase 2** must complete before user story implementation.
- **US1** is the MVP because formula and level 1 behavior underpin all later calculations.
- **US2** depends on US1 formula behavior.
- **US3** depends on US1 formula behavior and US2 result shape.
- **US4** can follow once the result shape exists.
- **Validation** runs after all selected user stories are implemented.

## Parallel Opportunities

- T001, T002, T003, and T005 can be performed in parallel.
- Test tasks are sequential in the checklist because they modify the same test file.
- Shared model tasks T006, T007, and T008 can be performed in parallel before T009 if coordinated.

## Implementation Strategy

1. Complete inspection tasks and confirm the current XP service layering.
2. Add Shared models, Domain interface, Infrastructure implementation shell, and DI registration.
3. Implement US1 formula and no-level-up behavior first.
4. Add multi-level progression state, next-level requirement, and unlock hook incrementally.
5. Run build and relevant tests.
