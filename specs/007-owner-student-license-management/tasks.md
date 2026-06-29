# Tasks: Owner Student License Management

**Input**: Design documents from `specs/007-owner-student-license-management/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/student-licenses-api.openapi.yaml](./contracts/student-licenses-api.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Required for this feature because the plan identifies quota accounting, Owner authorization, same-community grade/class validation, duplicate email prevention, email-change limits, and revocation access cleanup as non-trivial logic.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently after the shared schema foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase because it touches different files and has no dependency on incomplete work.
- **[Story]**: Maps task to a user story from [spec.md](./spec.md).
- Every task includes exact file paths.

## Phase 1: Setup (Shared Inspection)

**Purpose**: Confirm existing SprintLabs patterns before editing code.

- [x] T001 Inspect existing community controller routes and claim extraction in `API/Controllers/CommunitiesController.cs`
- [x] T002 Inspect existing student-adjacent entities and enums in `Domain/Models/Community.cs`, `Domain/Models/Player.cs`, and `Domain/Enums/CommunityEnums.cs`
- [x] T003 Inspect EF Core configuration for communities, grades, classes, users, and players in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T004 Inspect generic repository and community access patterns in `Domain/Repositories/IBaseRepository.cs`, `Infrastructure/Repositories/BaseRepository.cs`, `Domain/Services/ICommunityAccessService.cs`, and `Infrastructure/Services/CommunityAccessService.cs`
- [x] T005 Inspect owner-only handler patterns in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs`, `Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileCommandHandler.cs`, and `Application/Features/Communities/GradesClasses/CreateClass/CreateClassCommandHandler.cs`
- [x] T006 Inspect handler test patterns in `SprintLabs.Tests/Features/OwnerTeacherManagement/`, `SprintLabs.Tests/Features/OwnerCommunityProfile/`, and `SprintLabs.Tests/Features/CommunityGradesClasses/`

---

## Phase 2: Foundational (Blocking Schema and Shared Types)

**Purpose**: Add the shared data model, EF configuration, migration, and common DTOs required by all user stories.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [x] T007 Add `StudentLicenseStatus` enum with `Pending`, `Active`, and `Revoked` in `Domain/Enums/CommunityEnums.cs`
- [x] T008 Add `StudentLicense` entity in `Domain/Models/StudentLicense.cs`
- [x] T009 Add optional `StudentLicenses` navigation collection to `Community` in `Domain/Models/Community.cs`
- [x] T010 Add `DbSet<StudentLicense>` to `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T011 Configure StudentLicense required fields, max lengths, defaults, indexes, and relationships in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [x] T012 Generate EF Core migration `OwnerStudentLicenseManagement` under `Infrastructure/Migrations/`
- [x] T013 Review generated migration and model snapshot for only StudentLicenses schema changes in `Infrastructure/Migrations/`
- [x] T014 Create shared student license response DTO in `Application/Features/Communities/StudentLicenses/Common/StudentLicenseResponse.cs`
- [x] T015 Create shared grade summary DTO in `Application/Features/Communities/StudentLicenses/Common/StudentLicenseGradeResponse.cs`
- [x] T016 Create shared class summary DTO in `Application/Features/Communities/StudentLicenses/Common/StudentLicenseClassResponse.cs`
- [x] T017 Create shared Owner authorization helper in `Application/Features/Communities/StudentLicenses/Common/StudentLicenseAuthorization.cs`
- [x] T018 Create shared test helper and seed data utilities in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/OwnerStudentLicenseManagementTestHelper.cs`

**Checkpoint**: StudentLicense model, EF configuration, migration, common DTOs, and Owner authorization helper exist and build-level schema scope is understood.

---

## Phase 3: User Story 1 - Add Student Licenses (Priority: P1) MVP

**Goal**: Active Owners can add Pending student licenses when capacity is available, with same-community grade/class validation and duplicate email protection.

**Independent Test**: Sign in as an Active Owner, add a student license with a valid email, grade, and class, and confirm the license is Pending, `UsedStudents` increments by one, no Student `CommunityUser` is created, and invalid capacity/assignment/duplicate cases are rejected.

### Tests for User Story 1

- [x] T019 [P] [US1] Add successful add-license and UsedStudents increment tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`
- [x] T020 [P] [US1] Add capacity rejection and missing CommunityLicense tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`
- [x] T021 [P] [US1] Add same-community grade/class and class-belongs-to-grade validation tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`
- [x] T022 [P] [US1] Add duplicate normalized non-revoked email and revoked-email-reuse tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`
- [x] T023 [P] [US1] Add non-Owner, inactive Owner, no-membership, suspended-user, and platform-admin-only denial tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`

### Implementation for User Story 1

- [x] T024 [US1] Create add student license request DTO in `Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseRequest.cs`
- [x] T025 [US1] Create add student license command in `Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseCommand.cs`
- [x] T026 [US1] Implement add student license handler with email validation, Owner authorization, capacity check, grade/class validation, duplicate check, Pending status, `AssignedByUserId`, and UsedStudents increment in `Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseCommandHandler.cs`
- [x] T027 [US1] Add POST `/api/v1/Communities/{communityId}/student-licenses` action returning `BaseResponse<StudentLicenseResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Story 1 is functional and independently testable as the MVP.

---

## Phase 4: User Story 2 - List Student Licenses (Priority: P2)

**Goal**: Active Owners can list community student licenses with optional status, grade, class, and email search filters.

**Independent Test**: Create licenses with different statuses, grades, classes, communities, and emails, then list with and without filters and confirm only matching licenses from the Owner's community are returned.

### Tests for User Story 2

- [x] T028 [P] [US2] Add list all route-community licenses tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs`
- [x] T029 [P] [US2] Add status, gradeId, classId, and email search filter tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs`
- [x] T030 [P] [US2] Add invalid filter and non-Owner authorization tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs`

### Implementation for User Story 2

- [x] T031 [US2] Create list student licenses query in `Application/Features/Communities/StudentLicenses/ListStudentLicenses/ListStudentLicensesQuery.cs`
- [x] T032 [US2] Implement list student licenses handler with Owner authorization, route-community scoping, optional filters, grade/class summaries, and assigned-by data in `Application/Features/Communities/StudentLicenses/ListStudentLicenses/ListStudentLicensesQueryHandler.cs`
- [x] T033 [US2] Add GET `/api/v1/Communities/{communityId}/student-licenses` action with `status`, `gradeId`, `classId`, and `search` query parameters in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Update Pending Student Licenses (Priority: P3)

**Goal**: Active Owners can correct Pending license emails within the email change limit and update valid same-community grade/class assignments without changing UsedStudents.

**Independent Test**: Create a Pending license, change email within the allowed limit, update to another valid grade/class pair, and confirm `EmailChangeCount` changes only when normalized email changes and `UsedStudents` is unchanged.

### Tests for User Story 3

- [x] T034 [P] [US3] Add pending email update success, normalization, and email-change-count tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`
- [x] T035 [P] [US3] Add email change limit, duplicate email, Active email change, and Revoked email change rejection tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`
- [x] T036 [P] [US3] Add grade/class assignment update and invalid grade/class rejection tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`
- [x] T037 [P] [US3] Add UsedStudents unchanged and non-Owner authorization tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`

### Implementation for User Story 3

- [x] T038 [US3] Create update student license request DTO in `Application/Features/Communities/StudentLicenses/UpdateStudentLicense/UpdateStudentLicenseRequest.cs`
- [x] T039 [US3] Create update student license command in `Application/Features/Communities/StudentLicenses/UpdateStudentLicense/UpdateStudentLicenseCommand.cs`
- [x] T040 [US3] Implement update student license handler with same-community license lookup, Pending-only email change, email-change limit, duplicate prevention, grade/class validation, `UpdatedAt`, and no UsedStudents mutation in `Application/Features/Communities/StudentLicenses/UpdateStudentLicense/UpdateStudentLicenseCommandHandler.cs`
- [x] T041 [US3] Add PATCH `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` action returning `BaseResponse<StudentLicenseResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Stories 1, 2, and 3 are independently functional.

---

## Phase 6: User Story 4 - Revoke Student Licenses (Priority: P4)

**Goal**: Active Owners can revoke Pending or Active licenses without hard deletion, release counted student seats exactly once, and remove matching Student community access for linked active licenses.

**Independent Test**: Revoke Pending, Active, and already Revoked licenses, then confirm status, UsedStudents accounting, no hard delete, and only matching Student `CommunityUser` access is marked Removed.

### Tests for User Story 4

- [x] T042 [P] [US4] Add Pending license revoke success, no-hard-delete, and UsedStudents decrement tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/RevokeStudentLicenseCommandHandlerTests.cs`
- [x] T043 [P] [US4] Add Active linked license revoke and matching Student CommunityUser removal tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/RevokeStudentLicenseCommandHandlerTests.cs`
- [x] T044 [P] [US4] Add already Revoked idempotency and UsedStudents-not-below-zero tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/RevokeStudentLicenseCommandHandlerTests.cs`
- [x] T045 [P] [US4] Add unrelated membership preservation and non-Owner authorization tests in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/RevokeStudentLicenseCommandHandlerTests.cs`

### Implementation for User Story 4

- [x] T046 [US4] Create revoke student license command in `Application/Features/Communities/StudentLicenses/RevokeStudentLicense/RevokeStudentLicenseCommand.cs`
- [x] T047 [US4] Implement revoke student license handler with same-community license lookup, Revoked status, idempotent behavior, UsedStudents decrement, linked Student CommunityUser removal, and `UpdatedAt` in `Application/Features/Communities/StudentLicenses/RevokeStudentLicense/RevokeStudentLicenseCommandHandler.cs`
- [x] T048 [US4] Add DELETE `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` action returning `BaseResponse<StudentLicenseResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: All user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish documentation, validation, and scope review across the feature.

- [x] T049 [P] Create frontend-facing API documentation in `specs/007-owner-student-license-management/api.md`
- [x] T050 [P] Create frontend implementation guidance in `specs/007-owner-student-license-management/frontend.md`
- [x] T051 Run `dotnet build SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [x] T052 Run `dotnet test SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [ ] T053 Validate manual scenarios from `specs/007-owner-student-license-management/quickstart.md`
- [x] T054 Review diff for no custom repository, no new permissions framework, no authentication rewrite, no student activation, no email sending, no bulk import, no dashboard, no analytics, no payments, no parent accounts, and no teacher or grade/class management changes in `specs/007-owner-student-license-management/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; start immediately.
- **Foundational (Phase 2)**: Depends on setup inspection; blocks all user stories because StudentLicense schema and shared DTOs are required everywhere.
- **User Stories (Phases 3-6)**: Depend on Foundational.
- **Polish (Phase 7)**: Depends on completed selected user stories.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational and is the MVP because it creates the actual student license records and quota accounting.
- **User Story 2 (P2)**: Starts after Foundational; practically benefits from US1-created data but can be tested with seeded licenses.
- **User Story 3 (P3)**: Starts after Foundational; can be tested with seeded licenses but naturally follows US1.
- **User Story 4 (P4)**: Starts after Foundational; can be tested with seeded licenses and must regression-check quota/access cleanup against US1 behavior.

### Within Each User Story

- Write focused tests first and confirm they fail for missing behavior.
- Add request/command/query DTOs before handlers.
- Implement handler logic before controller actions.
- Run story-specific tests before moving to the next priority.
- Keep controller code thin and keep business rules in handlers.

### Parallel Opportunities

- T001-T006 are inspection tasks and can be split across agents or developers.
- T014-T018 can run in parallel after entity/config decisions are complete.
- T019-T023 can run in parallel with T024-T026 if test and implementation authors coordinate file ownership.
- T028-T030 can run in parallel with T031-T032 after foundational schema files exist.
- T034-T037 can run in parallel with T038-T040 after shared DTOs exist.
- T042-T045 can run in parallel with T046-T047 after shared DTOs exist.
- T049 and T050 can run in parallel after endpoint behavior is settled.

---

## Parallel Example: User Story 1

```text
Task: "Add successful add-license and UsedStudents increment tests in SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs"
Task: "Create add student license request DTO in Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseRequest.cs"
Task: "Create add student license command in Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseCommand.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add status, gradeId, classId, and email search filter tests in SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs"
Task: "Create list student licenses query in Application/Features/Communities/StudentLicenses/ListStudentLicenses/ListStudentLicensesQuery.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add pending email update success, normalization, and email-change-count tests in SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs"
Task: "Create update student license request DTO in Application/Features/Communities/StudentLicenses/UpdateStudentLicense/UpdateStudentLicenseRequest.cs"
```

## Parallel Example: User Story 4

```text
Task: "Add Active linked license revoke and matching Student CommunityUser removal tests in SprintLabs.Tests/Features/OwnerStudentLicenseManagement/RevokeStudentLicenseCommandHandlerTests.cs"
Task: "Create revoke student license command in Application/Features/Communities/StudentLicenses/RevokeStudentLicense/RevokeStudentLicenseCommand.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup inspection.
2. Complete Phase 2 entity, EF configuration, migration, common DTOs, and Owner authorization helper.
3. Complete Phase 3 add student license workflow.
4. Run `dotnet build SprintLabs.sln` and add-license handler tests.
5. Validate student license creation and capacity rejection manually from [quickstart.md](./quickstart.md).

### Incremental Delivery

1. Add schema foundation.
2. Add US1 add license and quota accounting.
3. Add US2 list/filter licenses.
4. Add US3 pending license email and grade/class updates.
5. Add US4 revoke license and student access cleanup.
6. Finish docs, build, tests, migration review, and scope review.

### Notes

- Use existing `IBaseRepository<T>` and `AsQueryable()` for simple lookups.
- Do not rely on `GetByIdAsync` for `long` identifiers.
- Do not add a custom repository, permissions framework, student activation flow, email sender, bulk import, dashboard, analytics, payment logic, parent accounts, teacher management, or grade/class management.
- Keep route `communityId` as the tenant boundary and validate every license/grade/class id against it.
- Pending and Active licenses count toward `UsedStudents`; Revoked licenses do not.
