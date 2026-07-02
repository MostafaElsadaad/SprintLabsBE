# Tasks: Community Student Viewing

**Input**: Design documents from `specs/009-community-student-viewing/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/community-students-api.openapi.yaml, quickstart.md

**Tests**: Included because the feature has non-trivial SaaS authorization, tenant isolation, filtering, pagination, and detail access rules. Follow the existing focused handler test style in `SprintLabs.Tests`.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches a different file or is independent after prerequisites
- **[Story]**: Maps to user stories from `spec.md`
- Every task includes an exact file path

## Phase 1: Setup

**Purpose**: Confirm existing patterns and create the feature shell without changing behavior.

- [X] T001 Inspect existing controller routing and BaseResponse usage in `API/Controllers/CommunitiesController.cs`
- [X] T002 Inspect existing community role authorization patterns in `Application/Features/Communities/GradesClasses/Common/CommunityGradesClassesAuthorization.cs` and `Application/Features/Communities/StudentLicenses/Common/StudentLicenseAuthorization.cs`
- [X] T003 Inspect existing student license list/query patterns in `Application/Features/Communities/StudentLicenses/ListStudentLicenses/ListStudentLicensesQueryHandler.cs`
- [X] T004 Inspect existing pagination classes and repository paging support in `Shared/Requests/PagedRequest.cs`, `Shared/Responses/PagedResponse.cs`, and `Infrastructure/Repositories/BaseRepository.cs`
- [X] T005 Inspect existing test helper patterns in `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/OwnerStudentLicenseManagementTestHelper.cs`
- [X] T006 Create feature directories for application code under `Application/Features/Communities/Students/Common/`, `Application/Features/Communities/Students/ListStudents/`, and `Application/Features/Communities/Students/GetStudentDetail/`
- [X] T007 [P] Create test directory for this feature in `SprintLabs.Tests/Features/CommunityStudentViewing/`

---

## Phase 2: Foundational

**Purpose**: Shared DTOs, validation, authorization, and controller imports that block user story implementation.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T008 Create Owner/Teacher authorization helper using `ICommunityAccessService.HasCommunityRole` in `Application/Features/Communities/Students/Common/CommunityStudentAuthorization.cs`
- [X] T009 Create grade/class filter validation helper for route community ownership and class-to-grade matching in `Application/Features/Communities/Students/Common/CommunityStudentValidation.cs`
- [X] T010 [P] Create student list item DTO in `Application/Features/Communities/Students/Common/CommunityStudentListItemResponse.cs`
- [X] T011 [P] Create student detail DTO in `Application/Features/Communities/Students/Common/CommunityStudentDetailResponse.cs`
- [X] T012 [P] Create placeholder analytics DTO in `Application/Features/Communities/Students/Common/CommunityStudentAnalyticsResponse.cs`
- [X] T013 Add required using statements for Community Student Viewing features in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: Shared feature DTOs and authorization/validation helpers exist; user story queries can compile against them.

---

## Phase 3: User Story 1 - List Community Students (Priority: P1) MVP

**Goal**: Owners and Teachers can view a paginated roster sourced from student licenses in their community, including pending students with nullable user/player fields.

**Independent Test**: Sign in as an Active Owner or Teacher, request the list for a community with pending and active licenses, and confirm only that community's students are returned in `PagedResponse`.

### Tests for User Story 1

- [X] T014 [P] [US1] Add owner list success test with paged response and route-community isolation in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T015 [P] [US1] Add teacher list success test in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T016 [P] [US1] Add pending student nullable user/player fields test in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Create list students query inheriting or carrying `PagedRequest` fields in `Application/Features/Communities/Students/ListStudents/ListStudentsQuery.cs`
- [X] T018 [US1] Implement base list query handler authorization and `StudentLicense.CommunityId` scoping in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T019 [US1] Implement projection from student licenses to `CommunityStudentListItemResponse` with nullable user/player fields in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T020 [US1] Implement `PagedResponse<CommunityStudentListItemResponse>` creation using existing page number/page size behavior in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T021 [US1] Add `GET {communityId}/students` controller action returning `BaseResponse<PagedResponse<CommunityStudentListItemResponse>>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Story 1 is independently testable as the MVP student roster endpoint.

---

## Phase 4: User Story 2 - Filter, Search, and Page Students (Priority: P2)

**Goal**: Owners and Teachers can filter by grade, class, status, search text, and use pagination metadata to navigate larger rosters.

**Independent Test**: Create licenses across statuses, grades, classes, emails, users, and player names, then confirm each filter and pagination request returns only expected records.

### Tests for User Story 2

- [X] T022 [P] [US2] Add status filter tests for Pending, Active, and Revoked licenses in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T023 [P] [US2] Add grade and class filter tests including matching grade/class combination in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T024 [P] [US2] Add invalid grade/class ownership and mismatched grade/class rejection tests in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T025 [P] [US2] Add search tests for license email, linked user email/name, linked player name, and other-community exclusion in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T026 [P] [US2] Add pagination metadata and beyond-last-page tests in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Add optional `GradeId`, `ClassId`, `Status`, and `Search` properties to `Application/Features/Communities/Students/ListStudents/ListStudentsQuery.cs`
- [X] T028 [US2] Call grade/class validation before applying list filters in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T029 [US2] Apply status, grade, and class filters to the student license query in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T030 [US2] Apply search across student license email, linked user email/name, and linked player name in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T031 [US2] Bind query parameters for gradeId, classId, status, search, pageNumber, and pageSize in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Stories 1 and 2 both work, and list behavior matches the OpenAPI list contract.

---

## Phase 5: User Story 3 - View Student Detail (Priority: P3)

**Goal**: Owners and Teachers can view detail for a player profile only when a non-revoked student license links that player to the route community.

**Independent Test**: Request detail for a player linked to a non-revoked student license in the community and confirm profile, progression, license, grade/class, and placeholder analytics fields are returned.

### Tests for User Story 3

- [X] T032 [P] [US3] Add owner detail success test with profile, progression, license, grade/class, and analytics placeholder assertions in `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`
- [X] T033 [P] [US3] Add teacher detail success test in `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`
- [X] T034 [P] [US3] Add cross-community player profile not-found test in `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`
- [X] T035 [P] [US3] Add revoked license and missing player profile not-found tests in `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`

### Implementation for User Story 3

- [X] T036 [US3] Create get student detail query in `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQuery.cs`
- [X] T037 [US3] Implement detail query handler authorization and non-revoked route-community license lookup by `PlayerProfileId` in `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQueryHandler.cs`
- [X] T038 [US3] Project player, user, license, grade, class, and default analytics fields into `CommunityStudentDetailResponse` in `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQueryHandler.cs`
- [X] T039 [US3] Add `GET {communityId}/students/{playerProfileId}` controller action returning `BaseResponse<CommunityStudentDetailResponse>` in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: User Story 3 works independently and detail never leaks player profiles outside the route community.

---

## Phase 6: User Story 4 - Enforce Student Viewing Permissions (Priority: P4)

**Goal**: Students, inactive members, users without community membership, platform-admin-only users, and unauthenticated requests cannot view community student data.

**Independent Test**: Attempt list and detail as each denied role/status and confirm access is rejected while Owner/Teacher access remains intact.

### Tests for User Story 4

- [X] T040 [P] [US4] Add list denial tests for Student, Pending member, Removed member, no membership, and platform-admin-only user in `SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs`
- [X] T041 [P] [US4] Add detail denial tests for Student, Pending member, Removed member, no membership, and platform-admin-only user in `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`
- [X] T042 [P] [US4] Add controller authorization smoke tests or documented manual 401 checks for unauthenticated list/detail requests in `SprintLabs.Tests/Features/CommunityStudentViewing/CommunityStudentViewingControllerTests.cs`

### Implementation for User Story 4

- [X] T043 [US4] Ensure list handler uses only the shared Owner/Teacher authorization helper before querying roster data in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T044 [US4] Ensure detail handler uses only the shared Owner/Teacher authorization helper before querying student detail data in `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQueryHandler.cs`
- [X] T045 [US4] Confirm Communities controller actions remain under existing `[Authorize]` controller policy in `API/Controllers/CommunitiesController.cs`

**Checkpoint**: All user stories are independently functional with access rules enforced consistently.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, contract alignment, verification, and cleanup across all stories.

- [X] T046 [P] Create API documentation for frontend consumers in `specs/009-community-student-viewing/api.md`
- [X] T047 [P] Create frontend behavior guide in `specs/009-community-student-viewing/frontend.md`
- [X] T048 Review OpenAPI contract against implemented DTO names and routes in `specs/009-community-student-viewing/contracts/community-students-api.openapi.yaml`
- [X] T049 Review implementation for accidental student license mutation, new tables, migrations, new pagination models, new permission framework, or auth rewrites in `Application/Features/Communities/Students/ListStudents/ListStudentsQueryHandler.cs`
- [X] T050 Review implementation for accidental student license mutation, cross-community detail leakage, or real analytics calculation in `Application/Features/Communities/Students/GetStudentDetail/GetStudentDetailQueryHandler.cs`
- [X] T051 Run `dotnet build SprintLabs.sln` from repository root `K:\Projects\DotNet\SprintLabsbkp`
- [X] T052 Run `dotnet test SprintLabs.sln` from repository root `K:\Projects\DotNet\SprintLabsbkp`
- [X] T053 Validate quickstart scenarios in `specs/009-community-student-viewing/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP.
- **User Story 2 (Phase 4)**: Depends on US1 list query shape and pagination.
- **User Story 3 (Phase 5)**: Depends on Foundational and shared DTO/authorization helpers; can proceed after US1 if controller route conflicts are coordinated.
- **User Story 4 (Phase 6)**: Depends on list and detail handlers existing.
- **Polish (Phase 7)**: Depends on completed implementation tasks for the desired stories.

### User Story Dependencies

- **US1 List Community Students**: MVP; no dependency on other stories after Foundational.
- **US2 Filter, Search, and Page Students**: Builds on US1 list endpoint.
- **US3 View Student Detail**: Independent of US2 after Foundational, but shares controller and common DTO/authorization files.
- **US4 Enforce Student Viewing Permissions**: Hardens US1 and US3 authorization paths.

### Within Each User Story

- Tests should be written first and fail before implementation.
- DTOs and shared helpers from Foundational must compile before query handlers.
- Query handlers should be complete before controller actions are finalized.
- Each story should be validated at its checkpoint before moving to lower-priority work.

### Parallel Opportunities

- T007 can run in parallel with setup inspection tasks.
- T010, T011, and T012 can run in parallel after common folder creation.
- US1 tests T014, T015, and T016 can be drafted in parallel.
- US2 tests T022 through T026 can be drafted in parallel after US1 handler shape is known.
- US3 tests T032 through T035 can be drafted in parallel with US3 query files.
- US4 list and detail denial tests T040 and T041 can be drafted in parallel.
- Documentation tasks T046 and T047 can run in parallel after behavior is implemented.

## Parallel Example: User Story 2

```text
Task: "Add status filter tests for Pending, Active, and Revoked licenses in SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs"
Task: "Add grade and class filter tests including matching grade/class combination in SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs"
Task: "Add search tests for license email, linked user email/name, linked player name, and other-community exclusion in SprintLabs.Tests/Features/CommunityStudentViewing/ListStudentsQueryHandlerTests.cs"
```

Note: These all touch the same test file, so coordinate if multiple agents edit concurrently; the scenarios themselves are logically independent.

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup inspection.
2. Complete Phase 2 shared DTO, authorization, and validation helpers.
3. Add US1 tests for Owner/Teacher list success, community isolation, pagination, and pending nullable fields.
4. Implement US1 list query and controller endpoint.
5. Run the US1 tests and confirm `GET /api/v1/Communities/{communityId}/students` returns a paginated roster.

### Incremental Delivery

1. Deliver US1 as the MVP student roster endpoint.
2. Add US2 filters, search, validation, and pagination edge cases.
3. Add US3 student detail endpoint with placeholder analytics.
4. Add US4 access-denial coverage for list/detail.
5. Add API/frontend docs and run build/test/quickstart validation.

### Guardrails

- Do not add a new database table or migration.
- Do not create or mutate student licenses.
- Do not create a new pagination model.
- Do not add a new permissions framework.
- Do not rewrite authentication.
- Do not calculate real analytics.
- Do not return students from outside the route community.
