# Tasks: Community Grades and Classes

**Input**: Design documents from `specs/006-community-grades-classes/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/grades-classes-api.openapi.yaml](./contracts/grades-classes-api.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Required for this feature because the plan identifies schema changes, role authorization, community ownership validation, class soft delete, and grade class counts as non-trivial logic.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after the shared schema foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase because it touches different files and has no dependency on incomplete work.
- **[Story]**: Maps task to a user story from [spec.md](./spec.md). Setup, foundational, and polish tasks do not use story labels.
- Every task includes exact file paths.

## Phase 1: Setup (Shared Inspection)

**Purpose**: Confirm existing SprintLabs patterns before editing code.

- [x] T001 Inspect existing community controller routes and claim extraction in `API/Controllers/CommunitiesController.cs`
- [x] T002 Inspect existing community CQRS slices in `Application/Features/Communities/`
- [x] T003 Inspect existing community entities and enums in `Domain/Models/Community.cs` and `Domain/Enums/CommunityEnums.cs`
- [x] T004 Inspect EF Core community configuration in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T005 Inspect generic repository and community access patterns in `Domain/Repositories/IBaseRepository.cs`, `Infrastructure/Repositories/BaseRepository.cs`, `Domain/Services/ICommunityAccessService.cs`, and `Infrastructure/Services/CommunityAccessService.cs`
- [x] T006 Inspect handler test patterns in `SprintLabs.Tests/Features/OwnerCommunityProfile/`, `SprintLabs.Tests/Features/OwnerTeacherManagement/`, and `SprintLabs.Tests/Features/CommunityAccessFoundation/`

---

## Phase 2: Foundational (Blocking Schema and Shared Types)

**Purpose**: Add the shared data model, EF configuration, migration, and common DTOs required by all user stories.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [x] T007 Add `ClassStatus` enum with `Active` and `Deleted` in `Domain/Enums/CommunityEnums.cs`
- [x] T008 Add `Grade` and `Class` entities plus navigation collections to `Domain/Models/Community.cs`
- [x] T009 Add `DbSet<Grade>` and `DbSet<Class>` to `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T010 Configure Grade properties, indexes, and Community relationship in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T011 Configure Class properties, indexes, Community relationship, Grade relationship, and default `ClassStatus.Active` in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T012 Generate EF Core migration `CommunityGradesAndClasses` under `Infrastructure/Migrations/`
- [x] T013 Review generated migration and model snapshot for only Grades/Classes schema changes in `Infrastructure/Migrations/`
- [x] T014 Create shared grade response DTO in `Application/Features/Communities/GradesClasses/Common/GradeResponse.cs`
- [x] T015 Create shared class response DTO in `Application/Features/Communities/GradesClasses/Common/ClassResponse.cs`
- [x] T016 Extend `API/Controllers/CommunitiesController.cs` using declarations for the GradesClasses feature folders without adding endpoint behavior yet

**Checkpoint**: Grade/Class model, EF configuration, migration, and common response DTOs exist and build-level schema scope is understood.

---

## Phase 3: User Story 1 - Create and List Grades (Priority: P1) MVP

**Goal**: Active Owners and Teachers can create grades and list community grades with active class counts.

**Independent Test**: Sign in as an Active Owner or Teacher, create grades in one community, list grades, and verify only route-community grades are returned with class counts that exclude deleted classes.

### Tests for User Story 1

- [x] T017 [P] [US1] Add create grade success, input validation, and Owner/Teacher authorization tests in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateGradeCommandHandlerTests.cs`
- [x] T018 [P] [US1] Add create grade denial tests for Student, Pending member, Removed member, no membership, and platform-admin-only user in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateGradeCommandHandlerTests.cs`
- [x] T019 [P] [US1] Add list grades community scoping and active-class-count tests in `SprintLabs.Tests/Features/CommunityGradesClasses/ListGradesQueryHandlerTests.cs`

### Implementation for User Story 1

- [x] T020 [US1] Create grade request DTO in `Application/Features/Communities/GradesClasses/CreateGrade/CreateGradeRequest.cs`
- [x] T021 [US1] Create grade command in `Application/Features/Communities/GradesClasses/CreateGrade/CreateGradeCommand.cs`
- [x] T022 [US1] Implement create grade handler with current-user validation, suspended-user rejection, `HasCommunityRole(... Owner, Teacher)`, name/sort validation, and repository save in `Application/Features/Communities/GradesClasses/CreateGrade/CreateGradeCommandHandler.cs`
- [x] T023 [US1] Create list grades query in `Application/Features/Communities/GradesClasses/ListGrades/ListGradesQuery.cs`
- [x] T024 [US1] Implement list grades handler with Owner/Teacher authorization, community scoping, ordering by sort order then name, and class counts excluding `ClassStatus.Deleted` in `Application/Features/Communities/GradesClasses/ListGrades/ListGradesQueryHandler.cs`
- [x] T025 [US1] Add POST `/api/v1/Communities/{communityId}/grades` action returning `BaseResponse<GradeResponse>` in `API/Controllers/CommunitiesController.cs`
- [x] T026 [US1] Add GET `/api/v1/Communities/{communityId}/grades` action returning `BaseResponse<List<GradeResponse>>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Story 1 is functional and independently testable as the MVP.

---

## Phase 4: User Story 2 - Create and List Classes (Priority: P2)

**Goal**: Active Owners and Teachers can create classes under same-community grades and list non-deleted classes with an optional grade filter.

**Independent Test**: Create classes under multiple grades, list all classes, list by grade, confirm deleted classes are excluded, and confirm grade ids from other communities are rejected.

### Tests for User Story 2

- [x] T027 [P] [US2] Add create class success and same-community grade validation tests in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateClassCommandHandlerTests.cs`
- [x] T028 [P] [US2] Add create class rejection tests for missing grade, other-community grade, invalid input, and non-Owner/Teacher roles in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateClassCommandHandlerTests.cs`
- [x] T029 [P] [US2] Add list classes tests for all classes, optional grade filter, deleted-class exclusion, other-community exclusion, and invalid grade filter in `SprintLabs.Tests/Features/CommunityGradesClasses/ListClassesQueryHandlerTests.cs`

### Implementation for User Story 2

- [x] T030 [US2] Create class request DTO in `Application/Features/Communities/GradesClasses/CreateClass/CreateClassRequest.cs`
- [x] T031 [US2] Create class command in `Application/Features/Communities/GradesClasses/CreateClass/CreateClassCommand.cs`
- [x] T032 [US2] Implement create class handler with Owner/Teacher authorization, name validation, same-community grade validation, `ClassStatus.Active`, and repository save in `Application/Features/Communities/GradesClasses/CreateClass/CreateClassCommandHandler.cs`
- [x] T033 [US2] Create list classes query in `Application/Features/Communities/GradesClasses/ListClasses/ListClassesQuery.cs`
- [x] T034 [US2] Implement list classes handler with Owner/Teacher authorization, optional grade filter validation, community scoping, and `ClassStatus.Active` filtering in `Application/Features/Communities/GradesClasses/ListClasses/ListClassesQueryHandler.cs`
- [x] T035 [US2] Add POST `/api/v1/Communities/{communityId}/classes` action returning `BaseResponse<ClassResponse>` in `API/Controllers/CommunitiesController.cs`
- [x] T036 [US2] Add GET `/api/v1/Communities/{communityId}/classes?gradeId={gradeId}` action returning `BaseResponse<List<ClassResponse>>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Update Classes (Priority: P3)

**Goal**: Active Owners and Teachers can update a non-deleted class name and move it to another grade in the same community.

**Independent Test**: Update a class name, move it to a same-community grade, and confirm missing/deleted/other-community class or grade cases are rejected without mutation.

### Tests for User Story 3

- [x] T037 [P] [US3] Add update class success tests for rename and same-community grade move in `SprintLabs.Tests/Features/CommunityGradesClasses/UpdateClassCommandHandlerTests.cs`
- [x] T038 [P] [US3] Add update class rejection tests for missing class, other-community class, deleted class, missing grade, other-community grade, invalid input, and non-Owner/Teacher roles in `SprintLabs.Tests/Features/CommunityGradesClasses/UpdateClassCommandHandlerTests.cs`

### Implementation for User Story 3

- [x] T039 [US3] Create update class request DTO in `Application/Features/Communities/GradesClasses/UpdateClass/UpdateClassRequest.cs`
- [x] T040 [US3] Create update class command in `Application/Features/Communities/GradesClasses/UpdateClass/UpdateClassCommand.cs`
- [x] T041 [US3] Implement update class handler with Owner/Teacher authorization, class ownership validation, non-deleted validation, target grade same-community validation, name trim, `UpdatedAt`, and repository save in `Application/Features/Communities/GradesClasses/UpdateClass/UpdateClassCommandHandler.cs`
- [x] T042 [US3] Add PATCH `/api/v1/Communities/{communityId}/classes/{classId}` action returning `BaseResponse<ClassResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Stories 1, 2, and 3 are independently functional.

---

## Phase 6: User Story 4 - Soft Delete Classes (Priority: P4)

**Goal**: Active Owners and Teachers can soft delete a class so it is excluded from class lists and grade class counts without hard deletion.

**Independent Test**: Delete an Active class, verify its status becomes Deleted, confirm it remains stored, confirm it disappears from class lists, and confirm grade class counts exclude it.

### Tests for User Story 4

- [x] T043 [P] [US4] Add delete class success, idempotent deleted-class behavior, and no-hard-delete tests in `SprintLabs.Tests/Features/CommunityGradesClasses/DeleteClassCommandHandlerTests.cs`
- [x] T044 [P] [US4] Add delete class rejection tests for missing class, other-community class, invalid ids, and non-Owner/Teacher roles in `SprintLabs.Tests/Features/CommunityGradesClasses/DeleteClassCommandHandlerTests.cs`
- [x] T045 [P] [US4] Add cross-check tests proving deleted classes are excluded from list classes and grade class counts in `SprintLabs.Tests/Features/CommunityGradesClasses/DeleteClassCommandHandlerTests.cs`

### Implementation for User Story 4

- [x] T046 [US4] Create delete class command in `Application/Features/Communities/GradesClasses/DeleteClass/DeleteClassCommand.cs`
- [x] T047 [US4] Implement delete class handler with Owner/Teacher authorization, class ownership validation, `ClassStatus.Deleted`, `UpdatedAt`, idempotent no-hard-delete behavior, and repository save in `Application/Features/Communities/GradesClasses/DeleteClass/DeleteClassCommandHandler.cs`
- [x] T048 [US4] Add DELETE `/api/v1/Communities/{communityId}/classes/{classId}` action returning `BaseResponse<ClassResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: All user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish documentation, validation, and scope review across the feature.

- [x] T049 [P] Create frontend-facing API documentation in `specs/006-community-grades-classes/api.md`
- [x] T050 [P] Create frontend implementation guidance in `specs/006-community-grades-classes/frontend.md`
- [x] T051 Run `dotnet build SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [x] T052 Run `dotnet test SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [ ] T053 Validate manual scenarios from `specs/006-community-grades-classes/quickstart.md`
- [x] T054 Review diff for no custom repository, no new permissions framework, no authentication rewrite, no grade update/delete endpoints, and no student assignment/license/analytics/payment/import/dashboard scope

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; start immediately.
- **Foundational (Phase 2)**: Depends on setup inspection; blocks all user stories because the Grade/Class schema is shared.
- **User Stories (Phases 3-6)**: Depend on Foundational.
- **Polish (Phase 7)**: Depends on completed selected user stories.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and is the MVP because grades are required before classes can be organized.
- **User Story 2 (P2)**: Starts after Foundational; practically benefits from US1-created grades but can be tested with seeded grades.
- **User Story 3 (P3)**: Starts after Foundational; can be tested with seeded classes but naturally follows US2.
- **User Story 4 (P4)**: Starts after Foundational; can be tested with seeded classes and must be regression-checked against US1 and US2 list behavior.

### Within Each User Story

- Write focused tests first and confirm they fail for missing behavior.
- Add request/command/query DTOs before handlers.
- Implement handler logic before controller actions.
- Run story-specific tests before moving to the next priority.
- Keep controller code thin and keep business rules in handlers.

### Parallel Opportunities

- T001-T006 are inspection tasks and can be split across agents or developers.
- T014 and T015 can run in parallel after the schema model decision is complete.
- T017-T019 can run in parallel with T020-T024 if test and implementation authors coordinate file ownership.
- T027-T029 can run in parallel with T030-T034 after foundational schema files exist.
- T037-T038 can run in parallel with T039-T041 after class DTOs exist.
- T043-T045 can run in parallel with T046-T047 after class DTOs exist.
- T049 and T050 can run in parallel after endpoint behavior is settled.

---

## Parallel Example: User Story 1

```text
Task: "Add create grade success, input validation, and Owner/Teacher authorization tests in SprintLabs.Tests/Features/CommunityGradesClasses/CreateGradeCommandHandlerTests.cs"
Task: "Create grade request DTO in Application/Features/Communities/GradesClasses/CreateGrade/CreateGradeRequest.cs"
Task: "Create list grades query in Application/Features/Communities/GradesClasses/ListGrades/ListGradesQuery.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add create class success and same-community grade validation tests in SprintLabs.Tests/Features/CommunityGradesClasses/CreateClassCommandHandlerTests.cs"
Task: "Create class request DTO in Application/Features/Communities/GradesClasses/CreateClass/CreateClassRequest.cs"
Task: "Create list classes query in Application/Features/Communities/GradesClasses/ListClasses/ListClassesQuery.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add update class success tests for rename and same-community grade move in SprintLabs.Tests/Features/CommunityGradesClasses/UpdateClassCommandHandlerTests.cs"
Task: "Create update class request DTO in Application/Features/Communities/GradesClasses/UpdateClass/UpdateClassRequest.cs"
```

## Parallel Example: User Story 4

```text
Task: "Add delete class success, idempotent deleted-class behavior, and no-hard-delete tests in SprintLabs.Tests/Features/CommunityGradesClasses/DeleteClassCommandHandlerTests.cs"
Task: "Create delete class command in Application/Features/Communities/GradesClasses/DeleteClass/DeleteClassCommand.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup inspection.
2. Complete Phase 2 entity, EF configuration, migration, and common DTOs.
3. Complete Phase 3 grade create/list workflow.
4. Run `dotnet build SprintLabs.sln` and grade handler tests.
5. Validate grade creation/listing manually from [quickstart.md](./quickstart.md).

### Incremental Delivery

1. Add schema foundation.
2. Add US1 grade create/list.
3. Add US2 class create/list.
4. Add US3 class update.
5. Add US4 class soft delete.
6. Finish docs, build, tests, migration review, and scope review.

### Notes

- Use existing `IBaseRepository<T>` and `AsQueryable()` for simple lookups.
- Do not rely on `GetByIdAsync` for `long` identifiers.
- Do not add grade/class repositories, a permissions framework, student assignment, student licenses, analytics, import/export, payment, dashboard, or grade update/delete endpoints.
- Keep route `communityId` as the tenant boundary and validate every grade/class id against it.
- Deleted classes must be filtered out of list classes and grade class counts.
