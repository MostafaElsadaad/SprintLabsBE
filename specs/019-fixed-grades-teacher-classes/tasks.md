# Tasks: Fixed Grades, Teacher Classes, and Demo Community

**Input**: Design documents from `specs/019-fixed-grades-teacher-classes/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Focused tests are required by FR-061. Write or update the listed tests before the corresponding implementation and confirm the new assertions fail for the intended reason.

**Organization**: Tasks are grouped by user story. Shared fixed-grade persistence is foundational because Teacher assignments, Teacher filtering, StudentLicense validation, and demo data all depend on it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks when prerequisite tasks are complete and files do not overlap
- **[Story]**: Maps the task to a user story in `spec.md`
- Every task names the exact repository file or command target

---

## Phase 1: Setup and Focused Baseline

**Purpose**: Preserve the current branch state, establish the required focused baseline, and confirm existing licensing behavior before implementation edits.

- [X] T001 Review `specs/019-fixed-grades-teacher-classes/spec.md`, `specs/019-fixed-grades-teacher-classes/plan.md`, `specs/019-fixed-grades-teacher-classes/research.md`, `specs/019-fixed-grades-teacher-classes/data-model.md`, `specs/019-fixed-grades-teacher-classes/contracts/fixed-grades-teacher-classes-api.md`, and the current `git status`/`git diff`; preserve all existing feature artifacts and record any unexpected working-tree risk in `specs/019-fixed-grades-teacher-classes/tasks.md`
- [X] T002 Run and record the focused pre-edit baseline using `dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~CommunityGradesClasses|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~CommunityStudentViewing|FullyQualifiedName~OwnerStudentLicenseManagement|FullyQualifiedName~CommunityAccessFoundation|FullyQualifiedName~CurrentCommunityResolution|FullyQualifiedName~CommunityAuthentication"` without running the full solution suite
- [X] T003 Inspect the current accounting behavior in `Application/Features/Communities/StudentLicenses/AddStudentLicense/AddStudentLicenseCommandHandler.cs`, `Application/Features/Communities/StudentLicenses/RevokeStudentLicense/RevokeStudentLicenseCommandHandler.cs`, `Infrastructure/Services/TeacherInvitationService.cs`, `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommandHandler.cs`, and `Domain/Models/Community.cs`; record the authoritative `UsedStudents`, `UsedTeachers`, status, and Owner-consumption rules in `specs/019-fixed-grades-teacher-classes/tasks.md` before writing demo-counter code

**T001-T003 completion notes (2026-08-31)**:

- The focused baseline passed **166 tests** with **0 failures** and **2 existing guarded MySQL skips**. The repository’s actual test project is `SprintLabs.Tests/Compass.Tests.csproj`; the original task path did not exist. Pre-existing compiler warnings were reported but did not fail the baseline.
- `UsedStudents` increments for every newly issued StudentLicense (Pending or Active) and decrements only when a non-Revoked license is revoked; therefore it represents non-Revoked StudentLicenses.
- `UsedTeachers` increments when a Teacher membership is issued or restored (Pending or Active) and decrements when that Teacher membership becomes Removed; therefore it represents current Pending/Active Teacher memberships.
- Owner membership issuance does not read or change Teacher capacity. The demo seed must preserve these observed rules and must not create a new license-accounting rule.

**Checkpoint**: Baseline and current license semantics are recorded; implementation may begin without broad test execution.

---

## Phase 2: Foundational Fixed-Grade Persistence

**Purpose**: Establish the non-destructive Grade schema and backfill that all user stories depend on.

**⚠️ CRITICAL**: Complete this phase before any user-story implementation.

- [ ] T004 Add failing focused model/migration tests for nullable legacy `Grade.Value`, supported-value check metadata, unique `(CommunityId, Value)`, preservation of recognized IDs/references, missing-grade backfill, unsupported-row retention, idempotency, and concise fail-closed duplicate recognition in `SprintLabs.Tests/Features/CommunityGradesClasses/FixedCommunityGradeModelTests.cs` and `SprintLabs.Tests/Features/CommunityGradesClasses/FixedCommunityGradeMigrationTests.cs`
- [ ] T005 Add nullable `Value`, retain legacy `Name`/`SortOrder`, and configure the supported-value check plus unique community/value index in `Domain/Models/Community.cs` and `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [ ] T006 Generate and hand-review `Infrastructure/Migrations/<timestamp>_FixedCommunityGrades.cs`, its designer, and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`; implement exact trimmed recognition, ID-preserving updates, missing 7-12 inserts, unsupported-row retention, and a concise pre-DML duplicate-recognition failure that directs operators to `specs/019-fixed-grades-teacher-classes/quickstart.md` rather than embedding detailed diagnostics in the migration
- [ ] T007 Run the focused persistence tests in `SprintLabs.Tests/Features/CommunityGradesClasses/FixedCommunityGradeModelTests.cs` and `SprintLabs.Tests/Features/CommunityGradesClasses/FixedCommunityGradeMigrationTests.cs`; if the optional MySQL test configuration is unavailable, record the skip/limitation in `specs/019-fixed-grades-teacher-classes/tasks.md` without creating a new integration harness

**Checkpoint**: The database can represent six supported Grades per Community while safely retaining unsupported legacy rows.

---

## Phase 3: User Story 1 - Fixed Grades for Every Community (Priority: P1) 🎯 MVP

**Goal**: New and migrated Communities expose exactly Grades 7-12 as read-only integer reference data, and all new Grade-linked writes reject unsupported or foreign Grades.

**Independent Test**: Create a Community, list Grades as an Owner and Teacher, verify exactly one ordered row for values 7-12, verify Grade mutation is unavailable, verify unsupported legacy rows remain excluded/preserved, and verify new Class/StudentLicense writes reject unsupported or foreign Grades.

### Tests for User Story 1

- [ ] T008 [P] [US1] Update Grade fixtures and add exact-set/order/exclusion/fail-closed tests in `SprintLabs.Tests/Features/CommunityGradesClasses/CommunityGradesClassesTestHelper.cs` and `SprintLabs.Tests/Features/CommunityGradesClasses/ListGradesQueryHandlerTests.cs`
- [ ] T009 [P] [US1] Add new-community automatic Grade tests in `SprintLabs.Tests/Features/AdminCommunityManagement/CreateCommunityCommandHandlerTests.cs`
- [ ] T010 [P] [US1] Add supported/foreign/legacy Grade validation tests for new and updated Classes and StudentLicenses in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateClassCommandHandlerTests.cs`, `SprintLabs.Tests/Features/CommunityGradesClasses/UpdateClassCommandHandlerTests.cs`, `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`, and `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`
- [ ] T011 [P] [US1] Add the removed Grade POST and preserved server-owned Grade GET route assertions to `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`

### Implementation for User Story 1

- [ ] T012 [US1] Initialize canonical Grades 7-12 on the Community aggregate before the existing first save in `Application/Features/Admin/Communities/CreateCommunity/CreateCommunityCommandHandler.cs`
- [ ] T013 [US1] Change the fixed Grade DTO and list handler to return only `{ id, value }`, order 7-12, and fail closed unless the exact supported set exists in `Application/Features/Communities/GradesClasses/Common/GradeResponse.cs` and `Application/Features/Communities/GradesClasses/ListGrades/ListGradesQueryHandler.cs`
- [ ] T014 [US1] Remove the `POST /api/v1/Communities/grades` action from `API/Controllers/CommunitiesController.cs`, delete the obsolete files under `Application/Features/Communities/GradesClasses/CreateGrade/`, and remove `SprintLabs.Tests/Features/CommunityGradesClasses/CreateGradeCommandHandlerTests.cs` without changing Student or Platform Admin compatibility routes
- [ ] T015 [US1] Require the selected Grade to belong to the current Community and have Value 7-12 in `Application/Features/Communities/GradesClasses/CreateClass/CreateClassCommandHandler.cs`, `Application/Features/Communities/GradesClasses/UpdateClass/UpdateClassCommandHandler.cs`, and `Application/Features/Communities/StudentLicenses/Common/StudentLicenseValidation.cs`; preserve existing records tied to unsupported legacy Grades
- [ ] T016 [US1] Run the focused User Story 1 tests under `SprintLabs.Tests/Features/CommunityGradesClasses/`, `SprintLabs.Tests/Features/AdminCommunityManagement/`, `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/`, and `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`, then run `dotnet build SprintLabs.sln`

**Checkpoint**: User Story 1 is independently functional and is the MVP foundation for all remaining work.

---

## Phase 4: User Story 2 - Replace a Teacher's Class Assignments (Priority: P1)

**Goal**: An Active Owner atomically replaces an Active same-community Teacher's complete set of Active same-community supported-Grade Classes without client-supplied Community context.

**Independent Test**: Replace assignments with one, multiple, another set, duplicates, and an empty set; verify invalid/foreign/inactive requests change nothing and concurrent valid replacements commit complete non-merged sets with no duplicate pair.

### Tests for User Story 2

- [ ] T017 [P] [US2] Add mapping and uniqueness tests for `TeacherClassAssignment`, restricted foreign keys, and unique `(TeacherUserId, ClassId)` in `SprintLabs.Tests/Features/OwnerTeacherManagement/TeacherClassAssignmentModelTests.cs`
- [ ] T018 [P] [US2] Add handler tests for one/multiple/replace/empty/normalized duplicates, Owner-only authorization, Active Teacher/Class validation, supported Grade validation, tenant isolation, and all-or-nothing invalid requests in `SprintLabs.Tests/Features/OwnerTeacherManagement/ReplaceTeacherClassAssignmentsCommandHandlerTests.cs`
- [ ] T019 [P] [US2] Using the repository's existing guarded MySQL fixture/configuration, add a provider-specific concurrency test proving every committed replacement is a complete transactionally consistent set, competing valid replacements are never partially merged, and no duplicate Teacher/Class pair exists in `SprintLabs.Tests/Features/OwnerTeacherManagement/TeacherClassAssignmentConcurrencyTests.cs`; when MySQL configuration is unavailable, follow the existing skip convention, record the limitation in `specs/019-fixed-grades-teacher-classes/tasks.md` or feature verification notes, do not create a new MySQL integration harness, and do not block unrelated focused tests
- [ ] T020 [P] [US2] Add the community-ID-free Owner-only PUT route contract and preserved Student/Admin route assertions in `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`

### Implementation for User Story 2

- [ ] T021 [US2] Add `TeacherClassAssignment` and the Class navigation in `Domain/Models/TeacherClassAssignment.cs` and `Domain/Models/Community.cs`; add its DbSet, restricted User/Class relationships, `ClassId` index, and unique pair index in `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [ ] T022 [US2] Generate and review `Infrastructure/Migrations/<timestamp>_TeacherClassAssignments.cs`, its designer, and `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` without adding CommunityId, GradeId, TeacherGrade, or a version column
- [ ] T023 [US2] Add the narrow transaction boundary following existing Infrastructure service conventions in `Domain/Services/ITeacherClassAssignmentService.cs`, `Infrastructure/Services/TeacherClassAssignmentService.cs`, and `Infrastructure/ServiceConfig.cs`; serialize replacements for one Teacher, validate the complete desired set inside the transaction, commit one normalized set with last-successful-write semantics, roll back invalid/transaction-conflict requests, and do not merge competing sets or add optimistic concurrency tokens
- [ ] T024 [P] [US2] Add `TeacherClassResponse` and extend the existing Teacher response with an initialized Classes collection in `Application/Features/Communities/Teachers/Common/TeacherClassResponse.cs` and `Application/Features/Communities/Teachers/Common/TeacherResponse.cs`
- [ ] T025 [US2] Add one-public-class-per-file request and command types for `{ classIds }` in `Application/Features/Communities/Teachers/ReplaceTeacherClassAssignments/ReplaceTeacherClassAssignmentsRequest.cs` and `Application/Features/Communities/Teachers/ReplaceTeacherClassAssignments/ReplaceTeacherClassAssignmentsCommand.cs`
- [ ] T026 [US2] Implement the MediatR handler with existing suspended-user and Active Owner authorization, tenant-safe errors, transactional service invocation, and mapped complete response in `Application/Features/Communities/Teachers/ReplaceTeacherClassAssignments/ReplaceTeacherClassAssignmentsCommandHandler.cs`
- [ ] T027 [US2] Add the thin `PUT /api/v1/Communities/teachers/{teacherUserId}/classes` action to `API/Controllers/CommunitiesController.cs`, resolving CommunityId from authenticated userId through the existing current-community resolver and never accepting CommunityId from the request
- [ ] T028 [US2] Run focused tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/TeacherClassAssignmentModelTests.cs`, `ReplaceTeacherClassAssignmentsCommandHandlerTests.cs`, `TeacherClassAssignmentConcurrencyTests.cs`, and `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`, then run `dotnet build SprintLabs.sln`

**Checkpoint**: User Story 2 is independently functional; assignments remain descriptive and grant no authorization.

---

## Phase 5: User Story 3 - Page and Filter the Teacher Roster (Priority: P1)

**Goal**: Owners receive an accurate database-paged Teacher roster with optional search/status/Grade/Class filters and bounded assignment/identity loading.

**Independent Test**: Verify page metadata, unassigned Teachers, class/Grade/combined filters, same-Grade de-duplication, returned Active Classes, tenant-safe invalid filters, Owner-only access, and no per-Teacher identity query pattern.

### Tests for User Story 3

- [ ] T029 [P] [US3] Rewrite focused roster tests for paging defaults/limits, search, status, no-filter visibility, class filter, Grade filter, combined match/mismatch, distinct Teachers, assigned-Class projection, tenant isolation, and bounded identity loading in `SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs`
- [ ] T030 [P] [US3] Update the GET Teacher route/response contract assertions for query parameters, `PagedResponse<TeacherResponse>`, server-owned CommunityId, Owner-only authorization, and unchanged explicit Admin scopes in `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`

### Implementation for User Story 3

- [ ] T031 [US3] Change `Application/Features/Communities/Teachers/ListTeachers/ListTeachersQuery.cs` to inherit `Shared.Requests.PagedRequest`, return `Shared.Responses.PagedResponse<TeacherResponse>`, and add optional GradeId, ClassId, Search, and CommunityUserStatus filters with a local maximum page size of 100
- [ ] T032 [US3] Replace in-memory/unpaged/N+1 roster logic with tenant-scoped IQueryable validation, assignment `Any` filters, database Count/Order/Skip/Take, one `IUserService.SearchUserIds` call when needed, one `IUserService.GetUsersByIds` page call, and one Active Class/Grade assignment query in `Application/Features/Communities/Teachers/ListTeachers/ListTeachersQueryHandler.cs`
- [ ] T033 [US3] Update the thin `GET /api/v1/Communities/teachers` action in `API/Controllers/CommunitiesController.cs` to bind paging/filter query parameters, retain server-owned current Community resolution, and wrap the paged result using existing `BaseResponse` conventions
- [ ] T034 [US3] Run `SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs` and the Teacher route assertions in `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`, then run `dotnet build SprintLabs.sln`

**Checkpoint**: User Story 3 is independently functional and uses assignments only for roster projection/filtering, never authorization.

---

## Phase 6: User Story 4 - Preserve the Existing Student Roster Experience (Priority: P2)

**Goal**: Existing Student pagination/filter/detail/tenant behavior remains unchanged while fixed Grades render compatibly and unsupported legacy records remain inspectable.

**Independent Test**: Run the existing Student roster/detail suite for paging, Grade, Class, combined filters, search, status, roles, and tenant isolation; verify supported Grades render numerically and an existing legacy-linked record remains readable.

### Tests for User Story 4

- [ ] T035 [P] [US4] Inspect the existing Student implementation and focused tests, keep every working test, and explicitly verify coverage for pagination/page metadata, gradeId, classId, combined gradeId+classId, mismatched grade/class safe failure, search, status, Owner access, Teacher access, foreign-community Grade/Class/Student/Profile, tenant isolation, Student detail, server-owned Community context, supported fixed-Grade numeric formatting, and readability of an existing unsupported legacy Grade-linked record in `SprintLabs.Tests/Features/CommunityStudentViewing/CommunityStudentViewingTestHelper.cs`, `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`, and `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`; add tests only for missing scenarios and do not redesign Student behavior
- [ ] T036 [P] [US4] Add StudentLicense response mapping assertions for supported integer display and preserved unsupported legacy display in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs`

### Implementation for User Story 4

- [ ] T037 [US4] Inspect the existing Student queries first and modify them only where T035/T036 exposes a fixed-Grade compatibility issue; format supported `Grade.Value` as invariant numeric text and fall back to retained legacy `Grade.Name` only for existing unsupported rows in `Application/Features/Communities/StudentLicenses/Common/StudentLicenseMapper.cs`, `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`, and `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQueryHandler.cs`; do not rewrite working Student queries, introduce StudentGrade, alter the existing StudentLicense/Class/Grade relationships, add strict supported-Grade checks to read filters, or replace server-owned Community context
- [ ] T038 [US4] Run focused tests under `SprintLabs.Tests/Features/CommunityStudentViewing/` and `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/ListStudentLicensesQueryHandlerTests.cs`, then run `dotnet build SprintLabs.sln`

**Checkpoint**: User Story 4 passes without a new Student placement model or loss of legacy remediation visibility.

---

## Phase 7: User Story 5 - Complete Development Demo School (Priority: P2)

**Goal**: An explicitly enabled Development startup reconciles one usable demo Community with fixed Grades, Classes, four staff accounts, overlapping Teacher assignments, 20 real Students, and license counters matching current domain rules.

**Independent Test**: Verify both seed gates, run the seed three times and from compatible partial state, log in all four staff users, inspect exact assignments and 20-Student distribution, validate Student list/detail usability, confirm no duplicates, and confirm counters follow the rules recorded in T003.

### Tests for User Story 5

- [ ] T039 [US5] Add focused tests for Development/config gating, complete dataset, fixed Grades/Classes, assignments, exact 20-Student distribution, current license-counter semantics, three-run idempotency, compatible partial reconciliation, and incompatible stable-key rollback in `SprintLabs.Tests/Features/DemoCommunitySeed/DemoCommunitySeederTests.cs`; using the existing realistic Community authentication test path rather than mocking away login, verify `owner.demo@sprintlabs.local`, `teacher1.demo@sprintlabs.local`, `teacher2.demo@sprintlabs.local`, and `teacher3.demo@sprintlabs.local` can each log in with `SprintLabsDemo!2026`, resolve to the expected seeded identity and exactly one current staff Community named `SprintLabs Demo School`, require no invitation acceptance, and leave normal non-seed authentication behavior unchanged

### Implementation for User Story 5

- [ ] T040 [US5] Add `DemoCommunitySeed.Enabled` defaulting to false in `API/appsettings.json` and invoke the seeder after migrations/current platform-admin seed only when both Development and the setting are true in `API/Program.cs`
- [ ] T041 [US5] Implement transactional stable-key reconciliation for the Active demo Community, six supported Grades, Classes 7A/7B/8A/8B/9A/10A, CommunityLicense capacity, and incompatible-state failures in `Infrastructure/Seed/DemoCommunitySeeder.cs`
- [ ] T042 [US5] Reconcile the Development-only Owner `owner.demo@sprintlabs.local` and Teachers `teacher1.demo@sprintlabs.local`, `teacher2.demo@sprintlabs.local`, and `teacher3.demo@sprintlabs.local`, each with password `SprintLabsDemo!2026`, through `UserManager<User>` and existing Identity mechanisms in `Infrastructure/Seed/DemoCommunitySeeder.cs`; make them confirmed Active accounts with existing Teacher eligibility flags, exactly one Active membership in SprintLabs Demo School, and the specified overlapping assignments, never manually hash passwords, and do not weaken normal invitation, authentication, or staff-community invariants
- [ ] T043 [US5] Reconcile exactly 20 deterministic student Identity/Player/Active Student membership/Active StudentLicense records with `PlayerProfileId`, supported Grade, Active Class, and assigned-by Owner links using the 4/4/4/3/3/2 class distribution in `Infrastructure/Seed/DemoCommunitySeeder.cs`
- [ ] T044 [US5] Reconcile `UsedStudents`, `UsedTeachers`, maximums, included statuses, and any Owner consumption in `Infrastructure/Seed/DemoCommunitySeeder.cs` strictly from the current rules recorded in T003; fail on incompatible stable keys and never redefine licensing behavior or reduce compatible existing capacity
- [ ] T045 [US5] Run `SprintLabs.Tests/Features/DemoCommunitySeed/DemoCommunitySeederTests.cs` plus focused Community login/current-community resolver tests in `SprintLabs.Tests/Features/CommunityAuthentication/CommunityLoginCommandHandlerTests.cs` and `SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceTests.cs`, then run `dotnet build SprintLabs.sln`

**Checkpoint**: User Story 5 is independently usable in an explicitly enabled Development environment and inert everywhere else.

---

## Phase 8: Polish, Documentation, and Cross-Cutting Verification

**Purpose**: Align executable documentation with the implementation, preserve compatibility boundaries, and perform final focused verification.

- [ ] T046 [P] Reconcile implemented methods, roles, request/response examples, errors, fixed-Grade rules, Teacher paging/filters/classes, Student compatibility, and Development credentials in `specs/019-fixed-grades-teacher-classes/api.md`, `specs/019-fixed-grades-teacher-classes/frontend.md`, and `specs/019-fixed-grades-teacher-classes/contracts/fixed-grades-teacher-classes-api.md`
- [ ] T047 [P] Verify and refine the pre-migration audit/remediation SQL, migration order/rollback warning, focused commands, demo enablement, and end-to-end checks in `specs/019-fixed-grades-teacher-classes/quickstart.md`; keep detailed legacy diagnostics here rather than in the EF migration
- [ ] T048 [P] Add only targeted supersession notes for removed mutable-Grade behavior and preserve current-community/Student/Admin route guidance in the applicable `specs/006-community-grades-classes/api.md`, `specs/006-community-grades-classes/frontend.md`, `specs/018-current-community-resolution/api.md`, and `specs/018-current-community-resolution/frontend.md` files that exist in the repository
- [ ] T049 Run focused security/regression tests for current-community resolution, Owner assignment, Teacher invitation/activation/login/refresh, Student compatibility, and unchanged Platform Admin scopes under `SprintLabs.Tests/Features/CurrentCommunityResolution/`, `SprintLabs.Tests/Features/OwnerTeacherManagement/`, `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/`, `SprintLabs.Tests/Features/CommunityAuthentication/`, and relevant API contract tests
- [ ] T050 Perform the focused SC-007 Development measurement documented in `specs/019-fixed-grades-teacher-classes/quickstart.md`: prepare or reuse a deterministic fixture with at least 100 Teacher memberships and assignment details, request one page of up to 100 Teachers through the real roster handler/API in a normal Development environment, measure elapsed time with a target of at most two seconds, and record the environment/result in `specs/019-fixed-grades-teacher-classes/quickstart.md` or task completion notes; do not add benchmarking packages, CI performance gates, or a performance-test framework
- [ ] T051 Run `dotnet build SprintLabs.sln` and then the combined feature-focused command against `SprintLabs.Tests/SprintLabs.Tests.csproj`; run `dotnet test SprintLabs.sln` only if focused failures require broader diagnosis or the user explicitly requests it
- [ ] T052 Review `git diff --check`, `git status`, and the complete diff against `specs/019-fixed-grades-teacher-classes/spec.md`, `specs/019-fixed-grades-teacher-classes/plan.md`, and `specs/019-fixed-grades-teacher-classes/tasks.md`; confirm no communityId returned to staff HTTP contracts, no Student/Admin compatibility regression, no unsupported new Grade reference, no licensing-rule redefinition, and mark every genuinely completed checkbox in `specs/019-fixed-grades-teacher-classes/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Starts immediately; T002 must pass or any pre-existing failure must be recorded before edits.
- **Phase 2 (Foundation)**: Depends on Phase 1 and blocks every user story.
- **Phase 3 (US1)**: Depends on Phase 2 and establishes fixed Grade behavior used by all later stories.
- **Phase 4 (US2)**: Depends on US1 because assignment validation requires supported Grades.
- **Phase 5 (US3)**: Depends on US2 because roster filters/projections query TeacherClassAssignment.
- **Phase 6 (US4)**: Depends on US1; it may proceed in parallel with US2/US3 after fixed Grades are complete.
- **Phase 7 (US5)**: Depends on US1 and US2; the seed needs supported Grades and TeacherClassAssignment. It does not need US3 implementation to persist data.
- **Phase 8 (Polish)**: Depends on every selected user story.

### User Story Dependency Graph

```text
Foundation
    |
   US1 Fixed Grades
   / | \
 US2 US4 \
  |       \
 US3      US5
```

- **US1 (P1)**: MVP and shared prerequisite.
- **US2 (P1)**: Adds assignment replacement; prerequisite for US3 and US5.
- **US3 (P1)**: Adds paged/filterable roster on top of US2.
- **US4 (P2)**: Compatibility verification after US1; otherwise independent.
- **US5 (P2)**: Uses US1 + US2 domain state; independently verifiable through seed tests and existing APIs.

### Within Each User Story

- Write/update focused tests first and confirm the new assertions fail for the expected missing behavior.
- Add model/schema before persistence services, handlers, and controllers.
- Validate the full tenant-owned input set before mutation.
- Preserve existing CQRS authorization after server-side Community resolution.
- Run the story's focused tests and `dotnet build SprintLabs.sln` at its checkpoint.

### Parallel Opportunities

- T008-T011 can run in parallel after the Grade foundation exists.
- T017-T020 can run in parallel as test-first work; T024 can run beside the transaction-service implementation after T021.
- T029 and T030 can run in parallel before roster implementation.
- US4 can run in parallel with US2/US3 after US1.
- T046-T048 can run in parallel after implementation contracts stabilize.
- Tasks touching `API/Controllers/CommunitiesController.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, migration snapshot files, or `Infrastructure/Seed/DemoCommunitySeeder.cs` must remain sequential to avoid shared-file conflicts.

---

## Parallel Execution Examples

### User Story 1

```text
T008: Grade list/fixture tests
T009: New-community Grade tests
T010: Class and StudentLicense strict-reference tests
T011: Controller route contract tests
```

### User Story 2

```text
T017: Assignment model/constraint tests
T018: Replacement handler/security tests
T019: Guarded MySQL concurrency tests
T020: PUT route contract tests
```

### User Story 3

```text
T029: Roster behavior/query-count tests
T030: GET Teacher HTTP contract tests
```

### User Story 4

```text
T035: Student roster/detail compatibility tests
T036: StudentLicense mapping tests
```

### Final Documentation

```text
T046: Feature API/frontend/contracts
T047: Quickstart and legacy audit
T048: Targeted older-spec supersession notes
```

---

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational persistence.
2. Complete US1 fixed Grades and strict new references.
3. Stop and verify US1 independently with its focused tests and solution build.
4. This is the smallest deployable increment: fixed read-only Grades with safe migration and no mutable Grade API.

### Incremental Delivery

1. Foundation + US1: fixed Grades and strict writes.
2. US2: safe Owner assignment replacement.
3. US3: paged/filterable Teacher roster.
4. US4: verified Student compatibility.
5. US5: opt-in Development dataset.
6. Documentation and final focused regression review.

### Execution Discipline

- Work in task-ID order unless a task is explicitly marked `[P]` and its prerequisites are complete.
- Do not introduce Grade systems/catalogs, TeacherGrade, generic tenancy/transaction frameworks, new packages, authentication redesign, or unrelated refactors.
- Do not run the full solution test suite during normal development; use focused tests and the solution build as specified.
- Do not commit unless the user separately requests it.
