# Tasks: Student License Activation on Login

**Input**: Design documents from `specs/008-student-license-activation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/google-login-student-activation.openapi.yaml, quickstart.md

**Tests**: Included because the login activation path mutates SaaS access and seat-linked records, and the SprintLabs constitution requires focused tests for non-trivial logic.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches a different file or is independent after prerequisites
- **[Story]**: Maps to user stories from `spec.md`
- Every task includes an exact file path

## Phase 1: Setup

**Purpose**: Confirm the existing login, student-license, membership, and test patterns before editing.

- [ ] T001 Inspect current Google login handler and pending teacher activation in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T002 Inspect existing student license schema and EF configuration in `Domain/Models/StudentLicense.cs`, `Domain/Enums/CommunityEnums.cs`, and `Infrastructure/DataAccess/ApplicationDbContext.cs`
- [ ] T003 Inspect existing login activation test style in `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs`
- [ ] T004 Confirm no migration is needed by verifying `StudentLicense.UserId`, `StudentLicense.PlayerProfileId`, `StudentLicense.Status`, and `StudentLicense.ActivatedAt` already exist in `Domain/Models/StudentLicense.cs`

---

## Phase 2: Foundational

**Purpose**: Prepare the login handler and test harness for student license activation without changing public API shape.

**CRITICAL**: No user story work should begin until the handler dependency shape and tests can compile.

- [ ] T005 Add `IBaseRepository<StudentLicense>` dependency to `GoogleAuthenticationCommandHandler` constructor and fields in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T006 Update existing handler construction in `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs` to pass `BaseRepository<StudentLicense>`
- [ ] T007 [P] Create the student activation test directory and test class scaffold in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`

**Checkpoint**: Existing pending-teacher test setup can construct the updated login handler, and the new student activation test file exists.

---

## Phase 3: User Story 1 - Activate Pending Student License on Login (Priority: P1) MVP

**Goal**: A student with one or more Pending licenses signs in with a matching Google email and receives Active student licenses plus Active Student community access.

**Independent Test**: Create Pending student licenses for a Google email, execute the Google login handler, and verify the licenses are Active, linked to user/player profile, have activation dates, create or restore Student memberships, and do not change `UsedStudents`.

### Tests for User Story 1

- [ ] T008 [US1] Add test for activating a single Pending student license and creating Active Student membership in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T009 [US1] Add test for activating Pending student licenses across multiple communities in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T010 [US1] Add test for restoring a Removed Student membership instead of creating a duplicate in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`

### Implementation for User Story 1

- [ ] T011 [US1] Add normalized Google email lookup for Pending student licenses in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T012 [US1] Add private student activation logic that sets `UserId`, `PlayerProfileId`, `Status = Active`, `ActivatedAt`, and `UpdatedAt` in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T013 [US1] Add private membership create/restore logic for `CommunityUser.Role = Student` and `CommunityUser.Status = Active` in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T014 [US1] Call student activation after player profile find/create and before JWT generation in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T015 [US1] Ensure student activation saves `StudentLicense` and `CommunityUser` changes without updating `CommunityLicense.UsedStudents` in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`

**Checkpoint**: User Story 1 is independently testable through the login handler and should satisfy pending-license activation, multi-community activation, membership creation/restoration, and no student-seat increment.

---

## Phase 4: User Story 2 - Preserve Existing Login Behavior (Priority: P2)

**Goal**: Google login response and existing B2C player login behavior remain unchanged whether or not student activation runs, and pending teacher activation still works.

**Independent Test**: Execute Google login for a user without pending student licenses and for a user with both pending teacher memberships and pending student licenses, then verify the normal login response fields and teacher activation behavior remain intact.

### Tests for User Story 2

- [ ] T016 [US2] Add test confirming Google login without pending student licenses preserves JWT, `UserId`, `PlayerProfileId`, and player progression fields in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T017 [US2] Add test confirming pending Teacher activation still works when pending Student licenses are also activated in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`

### Implementation for User Story 2

- [ ] T018 [US2] Ensure existing pending teacher activation remains before player lookup and unchanged in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T019 [US2] Ensure login response assignment of access token, `UserId`, `PlayerProfileId`, email, name, picture, gold, experience, and level remains unchanged in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T020 [US2] Ensure the no-pending-student path returns without extra writes or exceptions in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`

**Checkpoint**: User Stories 1 and 2 both work, and existing Google/player/teacher login behavior remains compatible.

---

## Phase 5: User Story 3 - Keep Activation Idempotent (Priority: P3)

**Goal**: Repeated logins do not duplicate Student memberships, increase `UsedStudents`, or activate ignored license statuses.

**Independent Test**: Activate once, record membership count and `UsedStudents`, log in again, and verify state remains stable. Also verify Active and Revoked licenses are ignored.

### Tests for User Story 3

- [ ] T021 [US3] Add repeated-login idempotency test for no duplicate Student memberships and unchanged `UsedStudents` in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T022 [US3] Add test confirming Revoked student licenses are not activated in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T023 [US3] Add test confirming already Active student licenses are not reprocessed or counted again in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [ ] T024 [US3] Add test confirming an existing non-Student same-community membership is not overwritten or duplicated in `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`

### Implementation for User Story 3

- [ ] T025 [US3] Ensure activation query filters only `StudentLicenseStatus.Pending` records in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T026 [US3] Ensure existing Active Student membership is left unchanged during activation in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T027 [US3] Ensure existing non-Student membership is not overwritten and no duplicate membership is added in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T028 [US3] Ensure repeated login performs no activation work for already Active or Revoked student licenses in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`

**Checkpoint**: All user stories are independently functional and repeated login is safe.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verify the slice, update frontend-facing docs for the changed login side effect, and keep the implementation scoped.

- [ ] T029 [P] Create or update API documentation for this feature in `specs/008-student-license-activation/api.md`
- [ ] T030 [P] Create or update frontend documentation for this feature in `specs/008-student-license-activation/frontend.md`
- [ ] T031 Review implementation for accidental new endpoints, authentication rewrites, schema changes, `UsedStudents` increments, duplicate memberships, and broken pending-teacher activation in `Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs`
- [ ] T032 Run `dotnet build SprintLabs.sln` from repository root `K:\Projects\DotNet\SprintLabsbkp`
- [ ] T033 Run `dotnet test SprintLabs.sln` from repository root `K:\Projects\DotNet\SprintLabsbkp`
- [ ] T034 Validate quickstart scenarios in `specs/008-student-license-activation/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks user story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational and can be validated after or alongside User Story 1, but should not change response behavior.
- **User Story 3 (Phase 5)**: Depends on Foundational and the activation path from User Story 1.
- **Polish (Phase 6)**: Depends on completed implementation tasks for the desired stories.

### User Story Dependencies

- **US1 Activate Pending Student License on Login**: MVP; no dependency on US2 or US3 after Foundational.
- **US2 Preserve Existing Login Behavior**: Depends on Foundational; validates compatibility around the login path touched by US1.
- **US3 Keep Activation Idempotent**: Depends on the US1 activation path because it hardens repeat behavior and ignored statuses.

### Within Each User Story

- Tests should be written first and fail before implementation.
- Handler dependency wiring must compile before behavior tests can run.
- Student license state updates and membership updates should be implemented before login response compatibility cleanup.
- Each story should be validated at its checkpoint before moving to lower-priority work.

### Parallel Opportunities

- T007 can run after T005/T006 planning context is clear because it creates a new test file.
- T029 and T030 can run in parallel after implementation behavior is known.
- Within manual validation, quickstart scenarios can be split by scenario after build and tests pass.

---

## Parallel Example: User Story 1

```text
Task: "Add test for activating a single Pending student license and creating Active Student membership in SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs"
Task: "Add normalized Google email lookup for Pending student licenses in Application/Features/Accounts/GoogleAuthenticate/GoogleAuthenticationCommandHandler.cs"
```

Note: These touch different files, but implementation should still wait for the intended failing test result before the handler change is finalized.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup inspection.
2. Complete Phase 2 dependency and test-harness wiring.
3. Add US1 tests for activation, multi-community activation, and Removed membership restore.
4. Implement US1 login activation after player profile resolution.
5. Run the US1 tests and confirm `UsedStudents` is unchanged.

### Incremental Delivery

1. Deliver US1 activation as the MVP.
2. Add US2 compatibility tests and confirm B2C player login and pending teacher activation remain stable.
3. Add US3 idempotency and ignored-status tests.
4. Add API/frontend docs and run build/test/quickstart validation.

### Guardrails

- Do not add a new endpoint.
- Do not add a migration unless required fields are unexpectedly missing.
- Do not increment or recalculate `CommunityLicense.UsedStudents` during activation.
- Do not overwrite Owner or Teacher community roles during student activation.
- Do not change login response contract unless a later explicit task requests it.
