# Tasks: Admin Community Foundation

**Input**: Design documents from `/specs/002-admin-community-foundation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/admin-communities.openapi.yaml, quickstart.md

**Tests**: Focused tests are included because the plan and constitution call for tests around non-trivial authorization, duplicate, idempotency, and license upsert behavior.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inspect current patterns and prepare the feature folder layout without changing behavior.

- [ ] T001 Inspect existing controller, CQRS, repository, EF, auth, BaseResponse, and GenericException patterns in API/Controllers/UsersController.cs, API/Controllers/AccountController.cs, Application/Features/Users/, Application/Features/Questions/, Domain/Repositories/, Infrastructure/DataAccess/ApplicationDbContext.cs, Infrastructure/Repositories/, Infrastructure/Services/UserService.cs, and API/Exceptions/GlobalExceptionHandler.cs
- [ ] T002 Create admin community feature folders in Application/Features/Admin/Communities/CreateCommunity/, Application/Features/Admin/Communities/ListCommunities/, Application/Features/Admin/Communities/AssignOwner/, Application/Features/Admin/Communities/UpsertCommunityLicense/, and Application/Features/Admin/Communities/Common/
- [ ] T003 [P] Create test folder for focused admin community tests in SprintLabs.Tests/Features/AdminCommunityFoundation/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain, persistence, repository, and admin/user support required before any story endpoint can work.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 [P] Add CommunityStatus enum with Active and Suspended values in Domain/Enums/CommunityStatus.cs
- [ ] T005 [P] Add CommunityUserRole enum with Owner, Teacher, and Student values in Domain/Enums/CommunityUserRole.cs
- [ ] T006 [P] Add CommunityUserStatus enum with Active, Pending, and Removed values in Domain/Enums/CommunityUserStatus.cs
- [ ] T007 [P] Add Community entity with Id, Name, Slug, Status, CreatedAt, UpdatedAt, CommunityUsers, and License members in Domain/Models/Community.cs
- [ ] T008 [P] Add CommunityUser entity with Id, CommunityId, UserId, Role, Status, CreatedAt, UpdatedAt, and Community navigation in Domain/Models/CommunityUser.cs
- [ ] T009 [P] Add CommunityLicense entity with Id, CommunityId, MaxStudents, UsedStudents, MaxTeachers, UsedTeachers, StudentEmailChangeLimit, CreatedAt, UpdatedAt, and Community navigation in Domain/Models/CommunityLicense.cs
- [ ] T010 Add CommunityUsers navigation to the existing User entity in Infrastructure/DataAccess/User.cs
- [ ] T011 Add DbSet properties for Communities, CommunityUsers, and CommunityLicenses in Infrastructure/DataAccess/ApplicationDbContext.cs
- [ ] T012 Configure Community, CommunityUser, CommunityLicense, User-to-CommunityUsers, unique Community.Slug, unique CommunityUser CommunityId+UserId, and unique CommunityLicense.CommunityId relationships in Infrastructure/DataAccess/ApplicationDbContext.cs
- [ ] T013 Add ICommunityRepository with community create, slug lookup, community existence, list with summaries, owner upsert, and license upsert methods in Domain/Repositories/ICommunityRepository.cs
- [ ] T014 Implement CommunityRepository over ApplicationDbContext with normalized slug/email-safe aggregate queries in Infrastructure/Repositories/CommunityRepository.cs
- [ ] T015 Register ICommunityRepository and CommunityRepository in Infrastructure/ServiceConfig.cs
- [ ] T016 Extend IUserService with platform-admin validation and find-or-create basic user by email contracts in Domain/Services/IUserService.cs
- [ ] T017 Implement platform-admin validation and basic owner user creation without GoogleId in Infrastructure/Services/UserService.cs
- [ ] T018 Add shared admin authorization helper behavior for commands/queries by using IUserService current-user validation in Application/Features/Admin/Communities/Common/AdminAuthorization.cs
- [ ] T019 Create EF Core migration AdminCommunityFoundation for Communities, CommunityUsers, CommunityLicenses, indexes, and defaults in Infrastructure/Migrations/

**Checkpoint**: Domain model, persistence, repository, user service support, and migration are ready for story slices.

---

## Phase 3: User Story 1 - Create a School Community (Priority: P1) MVP

**Goal**: Platform admins can create a community with a unique slug and Active status.

**Independent Test**: Sign in as platform admin, create a community with name and slug, verify Active status; repeat the slug and verify duplicate rejection; verify non-admin is rejected.

### Tests for User Story 1

- [ ] T020 [P] [US1] Add CreateCommunity handler tests for success, duplicate slug, missing admin user, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/CreateCommunityCommandHandlerTests.cs

### Implementation for User Story 1

- [ ] T021 [P] [US1] Add CreateCommunityRequest DTO with Name and Slug in Application/Features/Admin/Communities/CreateCommunity/CreateCommunityRequest.cs
- [ ] T022 [P] [US1] Add CommunityResponse DTO with Id, Name, Slug, and Status in Application/Features/Admin/Communities/Common/CommunityResponse.cs
- [ ] T023 [US1] Add CreateCommunityCommand carrying AuthenticatedUserId, Name, and Slug in Application/Features/Admin/Communities/CreateCommunity/CreateCommunityCommand.cs
- [ ] T024 [US1] Implement CreateCommunityCommandHandler with platform-admin check, slug normalization, duplicate rejection, Active status creation, and CommunityResponse mapping in Application/Features/Admin/Communities/CreateCommunity/CreateCommunityCommandHandler.cs
- [ ] T025 [US1] Add AdminCommunitiesController with api/v{version:apiVersion}/admin/communities route, ApiVersion 1.0, Authorize, IMediator injection, userId claim extraction, and POST create action in API/Controllers/AdminCommunitiesController.cs
- [ ] T026 [US1] Verify POST /api/v1/admin/communities response and error behavior against specs/002-admin-community-foundation/contracts/admin-communities.openapi.yaml

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 - List School Communities (Priority: P2)

**Goal**: Platform admins can list communities with owner and license summaries when available.

**Independent Test**: Create communities with and without owners/licenses, list as platform admin, and verify all communities return with correct nullable summaries.

### Tests for User Story 2

- [ ] T027 [P] [US2] Add ListCommunities handler tests for platform-admin success, no owner summary, no license summary, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/ListCommunitiesQueryHandlerTests.cs

### Implementation for User Story 2

- [ ] T028 [P] [US2] Add OwnerSummaryResponse DTO in Application/Features/Admin/Communities/Common/OwnerSummaryResponse.cs
- [ ] T029 [P] [US2] Add CommunityLicenseSummaryResponse DTO in Application/Features/Admin/Communities/Common/CommunityLicenseSummaryResponse.cs
- [ ] T030 [P] [US2] Add CommunityListItemResponse DTO in Application/Features/Admin/Communities/ListCommunities/CommunityListItemResponse.cs
- [ ] T031 [US2] Add ListCommunitiesQuery carrying AuthenticatedUserId in Application/Features/Admin/Communities/ListCommunities/ListCommunitiesQuery.cs
- [ ] T032 [US2] Implement ListCommunitiesQueryHandler with platform-admin check and repository summary mapping in Application/Features/Admin/Communities/ListCommunities/ListCommunitiesQueryHandler.cs
- [ ] T033 [US2] Add GET list action to AdminCommunitiesController using MediatR and BaseResponse<List<CommunityListItemResponse>> in API/Controllers/AdminCommunitiesController.cs
- [ ] T034 [US2] Verify GET /api/v1/admin/communities response fields against specs/002-admin-community-foundation/contracts/admin-communities.openapi.yaml

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Assign a Community Owner (Priority: P3)

**Goal**: Platform admins can assign or restore an active Owner membership by email without duplicate CommunityUser rows.

**Independent Test**: Assign an existing user as owner, assign a new email as owner, repeat the same assignment, and confirm a single active owner membership for that user/community.

### Tests for User Story 3

- [ ] T035 [P] [US3] Add AssignOwner handler tests for existing user, new basic user, repeated owner assignment idempotency, removed membership restoration, missing community, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/AssignOwnerCommandHandlerTests.cs

### Implementation for User Story 3

- [ ] T036 [P] [US3] Add AssignOwnerRequest DTO with Email and Name in Application/Features/Admin/Communities/AssignOwner/AssignOwnerRequest.cs
- [ ] T037 [P] [US3] Add OwnerResponse DTO with CommunityUserId, CommunityId, UserId, Email, Name, Role, and Status in Application/Features/Admin/Communities/AssignOwner/OwnerResponse.cs
- [ ] T038 [US3] Add AssignOwnerCommand carrying AuthenticatedUserId, CommunityId, Email, and Name in Application/Features/Admin/Communities/AssignOwner/AssignOwnerCommand.cs
- [ ] T039 [US3] Implement AssignOwnerCommandHandler with platform-admin check, community existence check, normalized email user lookup/create, Owner role assignment, Active status, and idempotent membership update in Application/Features/Admin/Communities/AssignOwner/AssignOwnerCommandHandler.cs
- [ ] T040 [US3] Add POST owner action to AdminCommunitiesController using MediatR and BaseResponse<OwnerResponse> in API/Controllers/AdminCommunitiesController.cs
- [ ] T041 [US3] Verify POST /api/v1/admin/communities/{communityId}/owner response and idempotency behavior against specs/002-admin-community-foundation/contracts/admin-communities.openapi.yaml

**Checkpoint**: User Stories 1, 2, and 3 work independently.

---

## Phase 6: User Story 4 - Configure Community License Limits (Priority: P4)

**Goal**: Platform admins can create or update community license limits while preserving usage counts.

**Independent Test**: Configure license limits for a community, update limits, confirm used counts remain unchanged, and reject limits below current usage.

### Tests for User Story 4

- [ ] T042 [P] [US4] Add UpsertCommunityLicense handler tests for create, update, preserve UsedStudents and UsedTeachers, reject limits below usage, reject negative values, missing community, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/UpsertCommunityLicenseCommandHandlerTests.cs

### Implementation for User Story 4

- [ ] T043 [P] [US4] Add UpsertCommunityLicenseRequest DTO with MaxStudents, MaxTeachers, and StudentEmailChangeLimit in Application/Features/Admin/Communities/UpsertCommunityLicense/UpsertCommunityLicenseRequest.cs
- [ ] T044 [P] [US4] Add CommunityLicenseResponse DTO with Id, CommunityId, MaxStudents, UsedStudents, MaxTeachers, UsedTeachers, and StudentEmailChangeLimit in Application/Features/Admin/Communities/UpsertCommunityLicense/CommunityLicenseResponse.cs
- [ ] T045 [US4] Add UpsertCommunityLicenseCommand carrying AuthenticatedUserId, CommunityId, MaxStudents, MaxTeachers, and StudentEmailChangeLimit in Application/Features/Admin/Communities/UpsertCommunityLicense/UpsertCommunityLicenseCommand.cs
- [ ] T046 [US4] Implement UpsertCommunityLicenseCommandHandler with platform-admin check, community existence check, non-negative validation, usage floor validation, create-if-missing, update-if-present, and usage preservation in Application/Features/Admin/Communities/UpsertCommunityLicense/UpsertCommunityLicenseCommandHandler.cs
- [ ] T047 [US4] Add PATCH licenses action to AdminCommunitiesController using MediatR and BaseResponse<CommunityLicenseResponse> in API/Controllers/AdminCommunitiesController.cs
- [ ] T048 [US4] Verify PATCH /api/v1/admin/communities/{communityId}/licenses response and preservation behavior against specs/002-admin-community-foundation/contracts/admin-communities.openapi.yaml

**Checkpoint**: All user stories are independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate architecture fit, forbidden scope, contracts, and build/test gates across the whole feature.

- [ ] T049 Review touched files for existing SprintLabs style, thin controllers, CQRS boundaries, repository boundaries, BaseResponse usage, GenericException usage, and no unrelated refactors in API/Controllers/AdminCommunitiesController.cs, Application/Features/Admin/Communities/, Domain/Models/, Domain/Enums/, Domain/Repositories/, Infrastructure/DataAccess/ApplicationDbContext.cs, Infrastructure/Repositories/CommunityRepository.cs, Infrastructure/Services/UserService.cs, and Shared/
- [ ] T050 Verify no teacher onboarding, student onboarding, grades, classes, student licenses, analytics, dashboards, invite emails, payment logic, or owner self-management artifacts were added by scanning API/, Application/, Domain/, Infrastructure/, Shared/, and SprintLabs.Tests/
- [ ] T051 Run dotnet build for the solution in SprintLabs.sln
- [ ] T052 Run dotnet test for the solution in SprintLabs.sln if the existing test project and database fixture can execute in the local environment
- [ ] T053 Execute quickstart validation scenarios and record any environment limitations in specs/002-admin-community-foundation/quickstart.md
- [ ] T054 Update specs/002-admin-community-foundation/tasks.md checkboxes as implementation tasks complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; provides MVP community creation.
- **User Story 2 (Phase 4)**: Depends on Foundational; can be implemented after or alongside US1 once repository foundations exist.
- **User Story 3 (Phase 5)**: Depends on Foundational and requires an existing community for manual validation.
- **User Story 4 (Phase 6)**: Depends on Foundational and requires an existing community for manual validation.
- **Polish (Phase 7)**: Depends on all desired stories being complete.

### User Story Dependencies

- **US1 Create a School Community**: First MVP slice after foundation.
- **US2 List School Communities**: Independent after foundation, but more useful after US1 creates data.
- **US3 Assign a Community Owner**: Independent after foundation, but manual validation needs a community from US1 or seeded data.
- **US4 Configure Community License Limits**: Independent after foundation, but manual validation needs a community from US1 or seeded data.

### Within Each User Story

- Tests should be written before implementation tasks when practical.
- DTOs and commands/queries should be added before handlers.
- Handlers should be implemented before controller actions.
- Contract verification should happen after endpoint implementation.
- Complete and validate each story before moving to the next priority unless intentionally parallelizing.

### Parallel Opportunities

- T003 can run in parallel with T001-T002.
- T004-T009 can run in parallel because they touch separate enum/entity files.
- T020 can run in parallel with T021-T022 after the foundational phase.
- T027 can run in parallel with T028-T030 after the foundational phase.
- T035 can run in parallel with T036-T037 after the foundational phase.
- T042 can run in parallel with T043-T044 after the foundational phase.
- US2, US3, and US4 can proceed in parallel after Foundational if developers coordinate edits to API/Controllers/AdminCommunitiesController.cs.

---

## Parallel Example: User Story 1

```text
Task: "Add CreateCommunity handler tests for success, duplicate slug, missing admin user, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/CreateCommunityCommandHandlerTests.cs"
Task: "Add CreateCommunityRequest DTO with Name and Slug in Application/Features/Admin/Communities/CreateCommunity/CreateCommunityRequest.cs"
Task: "Add CommunityResponse DTO with Id, Name, Slug, and Status in Application/Features/Admin/Communities/Common/CommunityResponse.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add ListCommunities handler tests for platform-admin success, no owner summary, no license summary, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/ListCommunitiesQueryHandlerTests.cs"
Task: "Add OwnerSummaryResponse DTO in Application/Features/Admin/Communities/Common/OwnerSummaryResponse.cs"
Task: "Add CommunityLicenseSummaryResponse DTO in Application/Features/Admin/Communities/Common/CommunityLicenseSummaryResponse.cs"
Task: "Add CommunityListItemResponse DTO in Application/Features/Admin/Communities/ListCommunities/CommunityListItemResponse.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add AssignOwner handler tests for existing user, new basic user, repeated owner assignment idempotency, removed membership restoration, missing community, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/AssignOwnerCommandHandlerTests.cs"
Task: "Add AssignOwnerRequest DTO with Email and Name in Application/Features/Admin/Communities/AssignOwner/AssignOwnerRequest.cs"
Task: "Add OwnerResponse DTO with CommunityUserId, CommunityId, UserId, Email, Name, Role, and Status in Application/Features/Admin/Communities/AssignOwner/OwnerResponse.cs"
```

## Parallel Example: User Story 4

```text
Task: "Add UpsertCommunityLicense handler tests for create, update, preserve UsedStudents and UsedTeachers, reject limits below usage, reject negative values, missing community, suspended admin user, and non-admin rejection in SprintLabs.Tests/Features/AdminCommunityFoundation/UpsertCommunityLicenseCommandHandlerTests.cs"
Task: "Add UpsertCommunityLicenseRequest DTO with MaxStudents, MaxTeachers, and StudentEmailChangeLimit in Application/Features/Admin/Communities/UpsertCommunityLicense/UpsertCommunityLicenseRequest.cs"
Task: "Add CommunityLicenseResponse DTO with Id, CommunityId, MaxStudents, UsedStudents, MaxTeachers, UsedTeachers, and StudentEmailChangeLimit in Application/Features/Admin/Communities/UpsertCommunityLicense/CommunityLicenseResponse.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: Create a School Community.
4. Stop and validate POST /api/v1/admin/communities independently.
5. Run dotnet build and the focused US1 tests before continuing.

### Incremental Delivery

1. Foundation ready: entities, EF configuration, migration, repository, user service support.
2. US1: Create communities and reject duplicate slugs.
3. US2: List communities with nullable owner/license summaries.
4. US3: Assign owners idempotently by email.
5. US4: Create/update license limits while preserving usage.
6. Polish: contract scan, forbidden-scope scan, build, tests, quickstart validation.

### Parallel Team Strategy

1. Complete Setup and Foundational tasks together.
2. After Foundational, split story slices:
   - Developer A: US1 create community
   - Developer B: US2 list communities
   - Developer C: US3 owner assignment
   - Developer D: US4 license upsert
3. Coordinate controller edits in API/Controllers/AdminCommunitiesController.cs.
4. Run the full build/test validation after integration.

## Notes

- [P] tasks touch separate files and can run in parallel when their phase dependencies are met.
- Every user-story task includes a [US#] label for traceability.
- All endpoint controller tasks must preserve the route contract from specs/002-admin-community-foundation/contracts/admin-communities.openapi.yaml.
- Do not implement teachers, students, grades, classes, student licenses, analytics, dashboards, invite emails, payments, or owner self-management in this feature.
