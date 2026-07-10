# Tasks: Progression Persistence Foundation

**Input**: Design documents from `specs/011-progression-persistence-foundation/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: No test tasks are included; this persistence-foundation pass is validated by migration review and `dotnet build`.

**Organization**: Tasks are ordered for a minimal persistence-only implementation.

## Phase 1: Inspection

**Purpose**: Confirm current persistence conventions before editing.

- [X] T001 Inspect current PlayerProfile entity in `Domain/Models/Player.cs`
- [X] T002 Inspect current DbContext and EF configuration style in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T003 Inspect existing enum conventions in `Domain/Enums/CommunityEnums.cs`

---

## Phase 2: Domain Enums

**Purpose**: Add progression enum values using current enum folder conventions.

- [X] T004 Add MatchType enum in `Domain/Enums/ProgressionEnums.cs`
- [X] T005 Add MatchStatus enum in `Domain/Enums/ProgressionEnums.cs`
- [X] T006 Add RankTier enum in `Domain/Enums/ProgressionEnums.cs`
- [X] T007 Add XpSourceType enum in `Domain/Enums/ProgressionEnums.cs`

---

## Phase 3: Domain Entities

**Purpose**: Add PlayerProfile progression fields and persistence entities.

- [X] T008 Add progression fields to PlayerProfile in `Domain/Models/Player.cs`
- [X] T009 Create Match entity in `Domain/Models/Progression.cs`
- [X] T010 Create MatchPlayer entity in `Domain/Models/Progression.cs`
- [X] T011 Create MatchQuestionResult entity in `Domain/Models/Progression.cs`
- [X] T012 Create MatchRewardResult entity in `Domain/Models/Progression.cs`
- [X] T013 Create PlayerXpLog entity in `Domain/Models/Progression.cs`
- [X] T014 Create PlayerRankLog entity in `Domain/Models/Progression.cs`

---

## Phase 4: EF Core Configuration

**Purpose**: Wire the new persistence model into EF Core using current inline DbContext style.

- [X] T015 Add DbSets in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T016 Configure EF Core relationships in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T017 Configure nullable CommunityId fields in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T018 Configure delete behavior safely for MySQL in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T019 Configure indexes in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T020 Configure unique MatchId + PlayerProfileId on MatchPlayers in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [X] T021 Configure unique MatchId + PlayerProfileId on MatchRewardResults in `Infrastructure/DataAccess/ApplicationDbContext.cs`

---

## Phase 5: Migration and Verification

**Purpose**: Generate and verify the persistence migration.

- [X] T022 Create AddProgressionPersistenceFoundation migration in `Infrastructure/Migrations/`
- [X] T023 Run dotnet build using `SprintLabs.sln`
- [X] T024 Verify migration shape in `Infrastructure/Migrations/*AddProgressionPersistenceFoundation*.cs`

---

## Dependencies & Execution Order

- Phase 1 must complete before edits.
- Phase 2 must complete before entity fields use the new enums.
- Phase 3 must complete before DbContext configuration.
- Phase 4 must complete before migration generation.
- Phase 5 completes validation.

## Parallel Opportunities

- T001, T002, and T003 can be inspected independently.
- T009 through T014 are separate entity definitions but share `Domain/Models/Progression.cs`, so edit sequentially.
- T015 through T021 all touch `Infrastructure/DataAccess/ApplicationDbContext.cs`, so edit sequentially.

## Implementation Strategy

1. Complete inspection tasks.
2. Add enums and domain entities.
3. Configure DbContext relationships, nullable community context, delete behavior, indexes, and unique constraints.
4. Generate the migration.
5. Build and inspect migration output before stopping.
