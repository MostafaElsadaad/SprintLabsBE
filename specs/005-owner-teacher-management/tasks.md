# Tasks: Owner Teacher Management

**Input**: Design documents from `specs/005-owner-teacher-management/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/teacher-management-api.openapi.yaml](./contracts/teacher-management-api.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Required for this feature because the plan identifies owner authorization, license seat accounting, duplicate prevention, removal state transitions, and login activation as non-trivial logic.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other tasks in the same phase because it touches different files and has no dependency on incomplete work.
- **[Story]**: Maps task to a user story from [spec.md](./spec.md). Setup, foundational, and polish tasks do not use story labels.
- Every task includes exact file paths.

## Phase 1: Setup (Shared Inspection)

**Purpose**: Confirm the existing SprintLabs patterns before editing code.

- [X] T001 Inspect existing member-facing controllers in `API/Controllers/CommunitiesController.cs` and `API/Controllers/UsersController.cs`
- [X] T002 Inspect existing Google login flow in the current auth command/handler files under `Application/Features/Auth/`
- [X] T003 Inspect existing community models/enums in `Domain/Models/CommunityUser.cs`, `Domain/Models/CommunityLicense.cs`, `Domain/Models/User.cs`, and `Domain/Enums/CommunityEnums.cs`
- [X] T004 Inspect generic repository and community access patterns in `Domain/Repositories/IBaseRepository.cs`, `Infrastructure/Repositories/BaseRepository.cs`, `Domain/Services/ICommunityAccessService.cs`, and `Infrastructure/Services/CommunityAccessService.cs`
- [X] T005 Inspect existing handler test patterns in `SprintLabs.Tests/Features/CommunityAccessFoundation/` and `SprintLabs.Tests/Features/OwnerCommunityProfile/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared DTO/request/controller shells needed by the owner teacher-management slices.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [X] T006 Create shared teacher response DTO in `Application/Features/Communities/Teachers/Common/TeacherResponse.cs`
- [X] T007 Create invite request DTO in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherRequest.cs`
- [X] T008 Create member-facing teacher controller shell with authenticated versioned route in `API/Controllers/CommunityTeachersController.cs`
- [X] T009 Add shared controller user-id claim extraction or reuse the existing controller-local pattern in `API/Controllers/CommunityTeachersController.cs`
- [X] T010 Verify no database schema change is required by checking `Domain/Models/CommunityUser.cs`, `Domain/Models/CommunityLicense.cs`, and `Infrastructure/DataAccess/ApplicationDbContext.cs`

**Checkpoint**: Shared DTO/request/controller structure exists, and no migration is planned unless the schema inspection proves required fields are missing.

---

## Phase 3: User Story 1 - Invite a Teacher Within License Capacity (Priority: P1) MVP

**Goal**: Active community Owners can invite a teacher by email/name, reserve exactly one teacher seat, create or restore a Pending Teacher membership, and avoid duplicate memberships.

**Independent Test**: Sign in as an Active Owner, invite a teacher when capacity is available, verify a Pending Teacher membership exists, `UsedTeachers` increments only when a counted seat is created/restored, and duplicate invites do not create duplicate rows or double-count seats.

### Tests for User Story 1

- [X] T011 [P] [US1] Add invite success and new-user creation tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`
- [X] T012 [P] [US1] Add invite authorization denial tests for non-owner, Pending Owner, Removed Owner, and platform-admin-without-owner in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`
- [X] T013 [P] [US1] Add invite capacity, missing license, duplicate invite, existing user, and Removed Teacher restore tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Create invite command in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommand.cs`
- [X] T015 [US1] Implement invite handler with `HasCommunityRole(..., Owner)`, normalized email lookup, user creation, license capacity check, membership create/restore, `UsedTeachers` increment, and `SaveChangesAsync` in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs`
- [X] T016 [US1] Add POST `/api/v1/Communities/{communityId}/teachers/invite` action dispatching `InviteTeacherCommand` and returning `BaseResponse<TeacherResponse>` in `API/Controllers/CommunityTeachersController.cs`
- [X] T017 [US1] Validate invite request fields and controlled errors for invalid email/name, missing capacity, full capacity, invalid community id, and unauthorized owner role in `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs`

**Checkpoint**: User Story 1 is functional and independently testable as the MVP.

---

## Phase 4: User Story 2 - List Community Teachers (Priority: P2)

**Goal**: Active community Owners can list Teacher memberships for their community with identity and membership status details.

**Independent Test**: Seed Teacher memberships with Pending, Active, and Removed statuses in one community plus teachers in another community, then verify an Active Owner receives only Teacher memberships for the requested community with user id, name, email, status, and created date.

### Tests for User Story 2

- [X] T018 [P] [US2] Add teacher list success, empty-list, status visibility, and community scoping tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs`
- [X] T019 [P] [US2] Add teacher list owner-authorization denial tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] Create list teachers query in `Application/Features/Communities/Teachers/ListTeachers/ListTeachersQuery.cs`
- [X] T021 [US2] Implement list teachers handler using `HasCommunityRole(..., Owner)`, `IBaseRepository<CommunityUser>.AsQueryable()`, user projection, Teacher role filtering, and community scoping in `Application/Features/Communities/Teachers/ListTeachers/ListTeachersQueryHandler.cs`
- [X] T022 [US2] Add GET `/api/v1/Communities/{communityId}/teachers` action returning `BaseResponse<List<TeacherResponse>>` in `API/Controllers/CommunityTeachersController.cs`
- [X] T023 [US2] Ensure list response maps `userId`, `name`, `email`, `status`, and `createdAt` without exposing entity navigation objects in `Application/Features/Communities/Teachers/ListTeachers/ListTeachersQueryHandler.cs`

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Remove a Teacher Without Hard Delete (Priority: P3)

**Goal**: Active community Owners can remove Teacher memberships by setting status to Removed, release teacher seats only for previously Pending or Active teachers, and never remove Owners through this endpoint.

**Independent Test**: Remove Pending and Active Teacher memberships and verify status becomes Removed, `UsedTeachers` decrements exactly once, repeated removal does not over-decrement, active community access is no longer granted, and Owner targets are rejected.

### Tests for User Story 3

- [X] T024 [P] [US3] Add remove Pending/Active Teacher success and seat decrement tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/RemoveTeacherCommandHandlerTests.cs`
- [X] T025 [P] [US3] Add already Removed Teacher, Owner target rejection, no-membership, and non-owner authorization tests in `SprintLabs.Tests/Features/OwnerTeacherManagement/RemoveTeacherCommandHandlerTests.cs`
- [X] T026 [P] [US3] Add removed teacher access denial verification using the existing community access service in `SprintLabs.Tests/Features/OwnerTeacherManagement/RemoveTeacherCommandHandlerTests.cs`

### Implementation for User Story 3

- [X] T027 [US3] Create remove teacher command in `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommand.cs`
- [X] T028 [US3] Implement remove teacher handler using `HasCommunityRole(..., Owner)`, Teacher role lookup, soft-remove status change, guarded `UsedTeachers` decrement, and `SaveChangesAsync` in `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommandHandler.cs`
- [X] T029 [US3] Add DELETE `/api/v1/Communities/{communityId}/teachers/{userId}` action returning `BaseResponse<TeacherResponse>` in `API/Controllers/CommunityTeachersController.cs`
- [X] T030 [US3] Ensure removal rejects invalid ids, missing teacher membership, Owner target memberships, and missing license with controlled errors in `Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommandHandler.cs`

**Checkpoint**: User Stories 1, 2, and 3 are independently functional.

---

## Phase 6: User Story 4 - Activate Pending Teacher on Matching Login (Priority: P4)

**Goal**: When a teacher signs in with the invited email, matching Pending Teacher memberships become Active without changing `UsedTeachers` and without breaking existing Google/player login behavior.

**Independent Test**: Create Pending Teacher memberships for a user email, execute the Google login path for that email, verify memberships become Active, `UsedTeachers` is unchanged, non-matching memberships remain Pending, and existing login response fields still return.

### Tests for User Story 4

- [X] T031 [P] [US4] Add pending teacher activation tests for one and multiple communities in `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs`
- [X] T032 [P] [US4] Add login compatibility tests confirming activation does not change `UsedTeachers` and does not remove existing Google/player login response fields in `SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs`

### Implementation for User Story 4

- [X] T033 [US4] Inspect and identify the exact Google login handler file under `Application/Features/Auth/` that creates or finds `User` by Google email
- [X] T034 [US4] Add pending Teacher membership activation after user resolution in the existing Google login handler under `Application/Features/Auth/`
- [X] T035 [US4] Ensure activation updates only `CommunityUser` rows where `UserId` matches the resolved user, `Role = Teacher`, and `Status = Pending` in the existing Google login handler under `Application/Features/Auth/`
- [X] T036 [US4] Ensure activation does not modify `CommunityLicense.UsedTeachers` and preserves existing JWT, UserId, PlayerProfileId, and login response behavior in the existing Google login handler under `Application/Features/Auth/`

**Checkpoint**: All user stories are independently functional and prior auth/player behavior is preserved.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish documentation, validation, and guardrails across the feature.

- [X] T037 [P] Create frontend-facing API documentation in `specs/005-owner-teacher-management/api.md`
- [X] T038 [P] Create frontend implementation guidance in `specs/005-owner-teacher-management/frontend.md`
- [X] T039 Run `dotnet build SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [X] T040 Run `dotnet test SprintLabs.sln` from repository root and record any environment-specific failure or workaround
- [ ] T041 Validate manual scenarios from `specs/005-owner-teacher-management/quickstart.md`
- [X] T042 Review diff for no new tables, no migration unless required by schema inspection, no custom repository, no permissions framework, no email sending, and no out-of-scope student/grade/class/license/dashboard/payment work

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; start immediately.
- **Foundational (Phase 2)**: Depends on setup inspection; blocks all user story implementation.
- **User Stories (Phases 3-6)**: Depend on Foundational.
- **Polish (Phase 7)**: Depends on completed selected user stories.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational. This is the MVP and provides invite/seat-reservation behavior.
- **User Story 2 (P2)**: Starts after Foundational. Can be implemented independently of US1 if teacher fixtures already exist.
- **User Story 3 (P3)**: Starts after Foundational. Can be implemented independently with pre-seeded Teacher memberships, but should be validated with US1-created memberships too.
- **User Story 4 (P4)**: Starts after Foundational. Does not require endpoint work but depends on understanding the same Teacher membership and seat-accounting rules.

### Within Each User Story

- Write focused tests first and confirm they fail for the missing behavior.
- Add command/query DTOs before handlers.
- Implement handler logic before controller actions.
- Run story-specific tests before moving to the next story.
- Keep controller code thin and keep business rules in handlers or the existing login handler.

### Parallel Opportunities

- T001-T005 are inspection tasks and can be split across agents or developers.
- T006 and T007 can run in parallel after inspection.
- T011-T013 can run in parallel because they add separate test cases to the same test file only if coordinated; otherwise do sequential edits to avoid merge conflicts.
- T018 and T019 can run in parallel with T020-T023 after the shared DTO exists if using separate files/branches.
- T024-T026 can run in parallel with T027-T030 after the shared DTO exists if using separate files/branches.
- T031 and T032 can run in parallel with endpoint work because login activation is a separate path.
- T037 and T038 can run in parallel after implementation behavior is settled.

---

## Parallel Example: User Story 1

```text
Task: "Add invite success and new-user creation tests in SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs"
Task: "Create invite command in Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommand.cs"
Task: "Create invite request DTO in Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherRequest.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add teacher list success, empty-list, status visibility, and community scoping tests in SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs"
Task: "Create list teachers query in Application/Features/Communities/Teachers/ListTeachers/ListTeachersQuery.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add remove Pending/Active Teacher success and seat decrement tests in SprintLabs.Tests/Features/OwnerTeacherManagement/RemoveTeacherCommandHandlerTests.cs"
Task: "Create remove teacher command in Application/Features/Communities/Teachers/RemoveTeacher/RemoveTeacherCommand.cs"
```

## Parallel Example: User Story 4

```text
Task: "Add pending teacher activation tests for one and multiple communities in SprintLabs.Tests/Features/OwnerTeacherManagement/PendingTeacherActivationTests.cs"
Task: "Inspect and identify the exact Google login handler file under Application/Features/Auth/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup inspection.
2. Complete Phase 2 shared DTO/controller structure.
3. Complete Phase 3 invite teacher workflow.
4. Run `dotnet build SprintLabs.sln` and the invite handler tests.
5. Validate the invite scenario manually from [quickstart.md](./quickstart.md).

### Incremental Delivery

1. Add US1 invite and capacity enforcement.
2. Add US2 owner teacher list.
3. Add US3 soft removal and seat release.
4. Add US4 login activation.
5. Finish docs, build, tests, and scope review.

### Notes

- Use existing `IBaseRepository<T>` and `AsQueryable()` for simple lookups.
- Do not rely on `GetByIdAsync` for `long` identifiers.
- Do not add `ICommunityRepository`, teacher repositories, a permissions framework, new tables, or email workflows.
- Keep `UsedTeachers` updates tied only to counted state transitions.
- Preserve existing Google login response and player-login behavior while adding activation.
