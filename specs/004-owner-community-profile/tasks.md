# Tasks: Owner Community Profile

**Input**: Design documents from `specs/004-owner-community-profile/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/communities-api.openapi.yaml](./contracts/communities-api.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Focused handler tests are included because membership isolation, Owner-only mutation, and duplicate-slug handling are non-trivial authorization and data-integrity behavior.

**Organization**: Tasks are grouped by user story so profile viewing can be completed and validated as the MVP before Owner editing is added.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and does not depend on incomplete work.
- **[Story]**: Maps the task to User Story 1 or User Story 2.
- Every task includes an exact file path.

---

## Phase 1: Setup and Pattern Inspection

**Purpose**: Confirm the current controller, CQRS, repository, authorization, error, and test patterns before adding files.

- [X] T001 Inspect member-facing routing, `userId` claim extraction, MediatR dispatch, and `BaseResponse` wrapping in `API/Controllers/UsersController.cs` and platform-admin route separation in `API/Controllers/AdminCommunitiesController.cs`
- [X] T002 [P] Inspect community access and long-id repository behavior in `Domain/Services/ICommunityAccessService.cs`, `Infrastructure/Services/CommunityAccessService.cs`, `Domain/Repositories/IBaseRepository.cs`, and `Infrastructure/Repositories/BaseRepository.cs`
- [X] T003 [P] Inspect community DTO, normalization, exception, and update conventions in `Application/Features/Admin/Communities/Common/CommunityResponse.cs`, `Application/Features/Admin/Communities/CreateCommunity/CreateCommunityCommandHandler.cs`, `Application/Features/Admin/Communities/UpsertCommunityLicense/UpsertCommunityLicenseCommandHandler.cs`, and `Shared/Enums/ErrorMessage.cs`
- [X] T004 [P] Inspect focused EF-backed handler test patterns in `SprintLabs.Tests/Features/CommunityAccessFoundation/` and confirm the new tests can reuse the existing in-memory test setup without adding packages

---

## Phase 2: Shared Foundation

**Purpose**: Add the response contract shared by both profile actions without changing entities, services, repositories, or schema.

**CRITICAL**: Complete this phase before either user story handler.

- [X] T005 Create `CommunityProfileResponse` with Id, Name, Slug, and Status in `Application/Features/Communities/Common/CommunityProfileResponse.cs`
- [X] T006 Verify the existing `Community` model and EF configuration already provide Name, Slug, Status, UpdatedAt, and unique slug support without edits in `Domain/Models/Community.cs` and `Infrastructure/DataAccess/ApplicationDbContext.cs`

**Checkpoint**: The shared DTO exists and no database or authorization foundation change is required.

---

## Phase 3: User Story 1 - View Community Profile (Priority: P1) MVP

**Goal**: An authenticated Active Owner, Teacher, or Student can view the selected community's basic profile, while inactive or absent memberships cannot.

**Independent Test**: Call `GET /api/v1/Communities/{communityId}` as Active Owner, Teacher, and Student users and receive the profile; verify Pending, Removed, no-membership, platform-admin-without-membership, suspended-user, and unauthenticated requests are rejected.

### Tests for User Story 1

- [X] T007 [P] [US1] Add `GetCommunityProfileQueryHandler` tests for Active Owner/Teacher/Student success, missing user, suspended user, no membership, Pending membership, Removed membership, missing community, and platform-admin-without-membership denial in `SprintLabs.Tests/Features/OwnerCommunityProfile/GetCommunityProfileQueryHandlerTests.cs`

### Implementation for User Story 1

- [X] T008 [P] [US1] Create `GetCommunityProfileQuery` carrying authenticated UserId and CommunityId in `Application/Features/Communities/GetCommunityProfile/GetCommunityProfileQuery.cs`
- [X] T009 [US1] Implement `GetCommunityProfileQueryHandler` with positive-id validation, current-user existence/status checks, `CanAccessCommunity`, long-id `AsQueryable().FirstOrDefaultAsync`, controlled errors, and DTO mapping in `Application/Features/Communities/GetCommunityProfile/GetCommunityProfileQueryHandler.cs`
- [X] T010 [US1] Add authorized, API-versioned `CommunitiesController` with `GET {communityId:long}`, exact `userId` claim extraction, MediatR dispatch, and `BaseResponse<CommunityProfileResponse>` wrapping in `API/Controllers/CommunitiesController.cs`
- [X] T011 [US1] Verify the implemented GET route, response fields, and 400/401/403/404 outcomes against `specs/004-owner-community-profile/contracts/communities-api.openapi.yaml`

**Checkpoint**: User Story 1 is independently functional and provides the minimum useful community profile experience.

---

## Phase 4: User Story 2 - Update Community Profile as Owner (Priority: P2)

**Goal**: Only an authenticated Active Owner can update the selected community's name and slug while preserving uniqueness and leaving Status unchanged.

**Independent Test**: PATCH as an Active Owner and confirm normalization, timestamp update, same-slug success, and persistence; verify Teacher, Student, inactive Owner, no-membership, platform-admin-without-membership, duplicate-slug, and invalid-input attempts do not mutate the profile.

### Tests for User Story 2

- [X] T012 [P] [US2] Add `UpdateCommunityProfileCommandHandler` tests for Owner success, trimming/lowercase normalization, UpdatedAt change, same-slug acceptance, duplicate-other-community rejection, empty input, missing community, Teacher/Student denial, Pending/Removed Owner denial, no-membership denial, suspended-user denial, platform-admin-without-membership denial, and no mutation on failure in `SprintLabs.Tests/Features/OwnerCommunityProfile/UpdateCommunityProfileCommandHandlerTests.cs`

### Implementation for User Story 2

- [X] T013 [P] [US2] Create required Name and Slug request fields in `Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileRequest.cs`
- [X] T014 [P] [US2] Create `UpdateCommunityProfileCommand` carrying authenticated UserId, CommunityId, Name, and Slug in `Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileCommand.cs`
- [X] T015 [US2] Implement `UpdateCommunityProfileCommandHandler` with current-user checks, positive-id/input validation, `HasCommunityRole` for Owner, long-id community lookup, normalized duplicate check excluding the current community, Name/Slug/UpdatedAt mutation, `UpdateAsync`, one `SaveChangesAsync`, controlled errors, and DTO mapping in `Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileCommandHandler.cs`
- [X] T016 [US2] Add `PATCH {communityId:long}` to `API/Controllers/CommunitiesController.cs`, mapping `UpdateCommunityProfileRequest` and authenticated `userId` into the command and returning `BaseResponse<CommunityProfileResponse>`
- [X] T017 [US2] Verify the implemented PATCH route, required request fields, response contract, same-slug behavior, and 400/401/403/404 outcomes against `specs/004-owner-community-profile/contracts/communities-api.openapi.yaml`

**Checkpoint**: Both user stories work independently, and profile mutation remains restricted to Active Owners.

---

## Phase 5: Polish and Cross-Cutting Validation

**Purpose**: Validate architecture fit, documentation, forbidden scope, and build/test gates across the feature.

- [X] T018 Review `API/Controllers/CommunitiesController.cs` and `Application/Features/Communities/` for thin-controller CQRS boundaries, DTO-only responses, existing `GenericException`/`ErrorMessage` usage, no duplicated membership query logic, and no platform-admin bypass
- [X] T019 Verify no entity, DbContext, migration, custom repository, permission framework, package, or platform-admin API change was introduced by reviewing `Domain/Models/Community.cs`, `Infrastructure/DataAccess/ApplicationDbContext.cs`, `Infrastructure/Migrations/`, `Domain/Repositories/`, `Infrastructure/Repositories/`, and `API/Controllers/AdminCommunitiesController.cs`
- [X] T020 Verify no teacher, student, grade, class, student-license, license-management, analytics, payment, dashboard, or member-management behavior was added by scanning `API/`, `Application/`, `Domain/`, `Infrastructure/`, and `Shared/`
- [X] T021 Update implemented route details, payloads, response examples, authorization, errors, and any implementation differences in `specs/004-owner-community-profile/api.md`
- [X] T022 [P] Update frontend roles, profile form behavior, loading/empty/error states, visibility rules, and API calls in `specs/004-owner-community-profile/frontend.md`
- [X] T023 Run `dotnet build SprintLabs.sln` from the repository root
- [X] T024 Run `dotnet test SprintLabs.sln` from the repository root and record any existing environment limitation if tests cannot execute
- [ ] T025 Execute the authorization, slug, persistence, and scope scenarios in `specs/004-owner-community-profile/quickstart.md` and record any environment-specific limitations
- [X] T026 Update completion checkboxes in `specs/004-owner-community-profile/tasks.md` after each implementation and validation task is completed

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 - Setup**: No dependencies.
- **Phase 2 - Shared Foundation**: Depends on pattern inspection and blocks both user stories.
- **Phase 3 - User Story 1**: Depends on the shared response DTO; delivers the MVP.
- **Phase 4 - User Story 2**: Depends on the shared response DTO and adds PATCH to the controller introduced by User Story 1.
- **Phase 5 - Polish**: Depends on both desired stories being implemented.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2 and has no dependency on profile editing.
- **User Story 2 (P2)**: Its command/handler can be developed after Phase 2, but controller integration depends on `CommunitiesController` from User Story 1.

### Within Each User Story

1. Add focused tests and confirm they fail for missing behavior.
2. Add the query/command and request DTOs.
3. Implement the handler using existing services and repositories.
4. Add or extend the controller endpoint.
5. Verify the contract and independent acceptance scenarios.

### Parallel Opportunities

- T002, T003, and T004 can run in parallel after T001 begins.
- T007 and T008 can run in parallel after Phase 2.
- T012, T013, and T014 can run in parallel after Phase 2.
- User Story 2 tests and application slice can be developed alongside User Story 1 if controller edits are coordinated.
- T022 can run in parallel with source review tasks after behavior is stable.

---

## Parallel Example: User Story 1

```text
Task: "Add GetCommunityProfileQueryHandler tests in SprintLabs.Tests/Features/OwnerCommunityProfile/GetCommunityProfileQueryHandlerTests.cs"
Task: "Create GetCommunityProfileQuery in Application/Features/Communities/GetCommunityProfile/GetCommunityProfileQuery.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add UpdateCommunityProfileCommandHandler tests in SprintLabs.Tests/Features/OwnerCommunityProfile/UpdateCommunityProfileCommandHandlerTests.cs"
Task: "Create UpdateCommunityProfileRequest in Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileRequest.cs"
Task: "Create UpdateCommunityProfileCommand in Application/Features/Communities/UpdateCommunityProfile/UpdateCommunityProfileCommand.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Setup and Shared Foundation.
2. Implement User Story 1 GET profile behavior.
3. Run its focused tests and manual membership scenarios.
4. Stop and validate the read-only community profile before adding mutation.

### Incremental Delivery

1. Deliver Active-member profile viewing.
2. Add Active-Owner profile editing with uniqueness safeguards.
3. Complete cross-cutting review, docs, build, tests, and quickstart validation.

### Parallel Team Strategy

1. Complete shared pattern inspection and response DTO together.
2. One developer handles GET query/handler/controller.
3. Another developer prepares PATCH tests/request/command/handler.
4. Coordinate the single `CommunitiesController.cs` edit before integration.

## Notes

- Do not use `IBaseRepository<Community>.GetByIdAsync`; Community IDs are `long` and the current method accepts `int`.
- Do not query `CommunityUser` directly in the new handlers; use `ICommunityAccessService`.
- Do not add a migration.
- Do not modify platform-admin authorization or routes.
- Keep both endpoints membership-based even when the caller is a platform admin.
