# Tasks: Community Access Foundation

**Input**: Design documents from `/specs/003-community-access-foundation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/users-communities.openapi.yaml, quickstart.md

**Tests**: Focused tests are included because the plan calls for tests around active/pending/removed/no-membership access and role-match behavior.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inspect current patterns and prepare feature structure without changing behavior.

- [ ] T001 Inspect existing user endpoints, admin community repository/service patterns, CQRS folder style, auth claim extraction, BaseResponse usage, and tests in API/Controllers/UsersController.cs, API/Controllers/AdminCommunitiesController.cs, Application/Features/Users/, Application/Features/Admin/Communities/, Domain/Repositories/ICommunityRepository.cs, Infrastructure/Repositories/CommunityRepository.cs, Domain/Services/, Infrastructure/Services/, and SprintLabs.Tests/Features/
- [ ] T002 Create community access feature folders in Application/Features/Users/GetCurrentUserCommunities/ and SprintLabs.Tests/Features/CommunityAccessFoundation/
- [ ] T003 [P] Confirm no EF model or migration changes are needed by comparing specs/003-community-access-foundation/data-model.md with Domain/Models/Community.cs and Infrastructure/DataAccess/ApplicationDbContext.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared repository and service behavior required by all user stories.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T004 Extend ICommunityRepository with active membership lookup, role lookup, and current-user active community list contracts in Domain/Repositories/ICommunityRepository.cs
- [ ] T005 Implement active membership lookup, role lookup, and current-user active community list queries in Infrastructure/Repositories/CommunityRepository.cs
- [ ] T006 Add ICommunityAccessService with CanAccessCommunity(userId, communityId) and HasCommunityRole(userId, communityId, roles) contracts in Domain/Services/ICommunityAccessService.cs
- [ ] T007 Implement CommunityAccessService using ICommunityRepository and Active membership semantics in Infrastructure/Services/CommunityAccessService.cs
- [ ] T008 Register ICommunityAccessService and CommunityAccessService in Infrastructure/ServiceConfig.cs

**Checkpoint**: Reusable community access and role-check primitives are ready for user stories.

---

## Phase 3: User Story 1 - Check Community Access (Priority: P1) MVP

**Goal**: Future endpoints can determine whether a user has Active membership in a community.

**Independent Test**: Create active, pending, removed, and missing memberships for a user, then verify CanAccessCommunity returns true only for Active membership.

### Tests for User Story 1

- [ ] T009 [P] [US1] Add CanAccessCommunity tests for Active, Pending, Removed, and missing membership cases in SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceTests.cs

### Implementation for User Story 1

- [ ] T010 [US1] Verify CommunityAccessService.CanAccessCommunity returns true only for Active CommunityUser rows in Infrastructure/Services/CommunityAccessService.cs
- [ ] T011 [US1] Verify CommunityRepository active membership query filters by exact UserId, CommunityId, and CommunityUserStatus.Active in Infrastructure/Repositories/CommunityRepository.cs

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 - Check Community Roles (Priority: P2)

**Goal**: Future endpoints can require Owner, Teacher, Student, or a set of those roles using one reusable role check.

**Independent Test**: Create active Owner, Teacher, and Student memberships, then verify HasCommunityRole succeeds only for matching required role sets and fails for inactive memberships or empty role sets.

### Tests for User Story 2

- [ ] T012 [P] [US2] Add HasCommunityRole tests for Owner, Teacher, Student, multi-role allowed sets, non-matching role, Pending/Removed matching role, and empty role list in SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceRoleTests.cs

### Implementation for User Story 2

- [ ] T013 [US2] Verify CommunityAccessService.HasCommunityRole rejects empty role inputs and requires Active membership in Infrastructure/Services/CommunityAccessService.cs
- [ ] T014 [US2] Verify CommunityRepository role lookup supports Owner, Teacher, and Student values without treating Users.IsPlatformAdmin as a membership role in Infrastructure/Repositories/CommunityRepository.cs

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - List My Communities (Priority: P3)

**Goal**: Authenticated users can retrieve only their own Active community memberships with community details and membership role/status.

**Independent Test**: Sign in as a user with Active, Pending, Removed, and other-user memberships, call GET /api/v1/Users/me/communities, and verify only the current user's Active memberships are returned.

### Tests for User Story 3

- [ ] T015 [P] [US3] Add GetCurrentUserCommunitiesQueryHandler tests for active memberships, Pending exclusion, Removed exclusion, other-user exclusion, and empty list in SprintLabs.Tests/Features/CommunityAccessFoundation/GetCurrentUserCommunitiesQueryHandlerTests.cs

### Implementation for User Story 3

- [ ] T016 [P] [US3] Add UserCommunityResponse DTO with CommunityId, Name, Slug, CommunityStatus, Role, and MembershipStatus in Application/Features/Users/GetCurrentUserCommunities/UserCommunityResponse.cs
- [ ] T017 [US3] Add GetCurrentUserCommunitiesQuery carrying authenticated UserId in Application/Features/Users/GetCurrentUserCommunities/GetCurrentUserCommunitiesQuery.cs
- [ ] T018 [US3] Implement GetCurrentUserCommunitiesQueryHandler to load only active memberships for the authenticated user and map DTOs in Application/Features/Users/GetCurrentUserCommunities/GetCurrentUserCommunitiesQueryHandler.cs
- [ ] T019 [US3] Add GET me/communities action to UsersController using existing userId claim extraction, MediatR, and BaseResponse<List<UserCommunityResponse>> in API/Controllers/UsersController.cs
- [ ] T020 [US3] Verify GET /api/v1/Users/me/communities response fields and 401 behavior against specs/003-community-access-foundation/contracts/users-communities.openapi.yaml

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate scope, architecture fit, build, tests, and quickstart behavior.

- [ ] T021 Review touched files for existing SprintLabs style, thin controller changes, CQRS boundaries, repository boundaries, service simplicity, BaseResponse usage, GenericException usage, and no unrelated refactors in API/Controllers/UsersController.cs, Application/Features/Users/GetCurrentUserCommunities/, Domain/Repositories/ICommunityRepository.cs, Domain/Services/ICommunityAccessService.cs, Infrastructure/Repositories/CommunityRepository.cs, Infrastructure/Services/CommunityAccessService.cs, Infrastructure/ServiceConfig.cs, and SprintLabs.Tests/Features/CommunityAccessFoundation/
- [ ] T022 Verify existing platform-admin APIs still use Users.IsPlatformAdmin and were not converted to community role authorization by scanning API/Controllers/AdminCommunitiesController.cs and Application/Features/Admin/Communities/
- [ ] T023 Verify no teacher management, student management, grades, classes, student licenses, analytics, payments, owner dashboard, admin community creation/licensing, schema changes, or migrations were added for this feature by scanning API/, Application/, Domain/, Infrastructure/, Shared/, and Infrastructure/Migrations/
- [ ] T024 Run dotnet build for the solution in SprintLabs.sln
- [ ] T025 Run dotnet test for the solution in SprintLabs.sln if the existing test project and database fixture can execute in the local environment
- [ ] T026 Execute quickstart validation scenarios and record any environment limitations in specs/003-community-access-foundation/quickstart.md
- [ ] T027 Update specs/003-community-access-foundation/tasks.md checkboxes as implementation tasks complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; provides MVP access check.
- **User Story 2 (Phase 4)**: Depends on Foundational and builds on the same active membership primitive as US1.
- **User Story 3 (Phase 5)**: Depends on Foundational; can be implemented after or alongside US1/US2 once repository methods exist.
- **Polish (Phase 6)**: Depends on all desired stories being complete.

### User Story Dependencies

- **US1 Check Community Access**: First MVP slice after foundation.
- **US2 Check Community Roles**: Independent after foundation but should reuse US1 active membership semantics.
- **US3 List My Communities**: Independent after foundation, but manual validation needs seeded or existing membership data.

### Within Each User Story

- Tests should be written before implementation tasks when practical.
- Repository query support before service behavior.
- Service behavior before future endpoint consumers.
- DTO/query before handler.
- Handler before controller action.
- Contract verification after endpoint implementation.

### Parallel Opportunities

- T003 can run in parallel with T001-T002.
- T009 can run in parallel with T010-T011 after Foundational.
- T012 can run in parallel with T013-T014 after Foundational.
- T015 can run in parallel with T016 after Foundational.
- US1, US2, and US3 can proceed in parallel after Foundational if developers coordinate edits to ICommunityRepository and CommunityRepository.

---

## Parallel Example: User Story 1

```text
Task: "Add CanAccessCommunity tests for Active, Pending, Removed, and missing membership cases in SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceTests.cs"
Task: "Verify CommunityAccessService.CanAccessCommunity returns true only for Active CommunityUser rows in Infrastructure/Services/CommunityAccessService.cs"
Task: "Verify CommunityRepository active membership query filters by exact UserId, CommunityId, and CommunityUserStatus.Active in Infrastructure/Repositories/CommunityRepository.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add HasCommunityRole tests for Owner, Teacher, Student, multi-role allowed sets, non-matching role, Pending/Removed matching role, and empty role list in SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceRoleTests.cs"
Task: "Verify CommunityAccessService.HasCommunityRole rejects empty role inputs and requires Active membership in Infrastructure/Services/CommunityAccessService.cs"
Task: "Verify CommunityRepository role lookup supports Owner, Teacher, and Student values without treating Users.IsPlatformAdmin as a membership role in Infrastructure/Repositories/CommunityRepository.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add GetCurrentUserCommunitiesQueryHandler tests for active memberships, Pending exclusion, Removed exclusion, other-user exclusion, and empty list in SprintLabs.Tests/Features/CommunityAccessFoundation/GetCurrentUserCommunitiesQueryHandlerTests.cs"
Task: "Add UserCommunityResponse DTO with CommunityId, Name, Slug, CommunityStatus, Role, and MembershipStatus in Application/Features/Users/GetCurrentUserCommunities/UserCommunityResponse.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational repository/service contracts.
3. Complete Phase 3: CanAccessCommunity behavior.
4. Stop and validate Active/Pending/Removed/missing membership checks.
5. Run dotnet build and focused tests before continuing.

### Incremental Delivery

1. Foundation ready: repository methods and community access service registered.
2. US1: CanAccessCommunity active membership checks.
3. US2: HasCommunityRole role-set checks.
4. US3: GET /api/v1/Users/me/communities.
5. Polish: scope scan, platform-admin separation scan, build, tests, quickstart validation.

### Parallel Team Strategy

1. Complete Setup and Foundational tasks together.
2. After Foundational, split story slices:
   - Developer A: US1 access checks
   - Developer B: US2 role checks
   - Developer C: US3 current-user community list endpoint
3. Coordinate repository interface/implementation edits.
4. Run full build/test validation after integration.

## Notes

- [P] tasks touch separate files and can run in parallel when phase dependencies are met.
- Every user-story task includes a [US#] label for traceability.
- No migration should be generated for this feature.
- Do not implement teacher management, student management, grades, classes, student licenses, analytics, payments, owner dashboard, or admin community creation/licensing in this feature.
