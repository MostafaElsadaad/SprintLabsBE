# Tasks: Trusted Current-Community Resolution

**Input**: Design documents from `/specs/018-current-community-resolution/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/current-community-api.md`, `quickstart.md`

**Tests**: Security-sensitive tests are required. Write or update the focused tests before their implementation task and confirm that each new assertion fails for the expected reason before changing production code.

**Organization**: Tasks are grouped by user story. CommunityId remains an internal MediatR value populated by the backend; no task introduces a tenant framework, JWT community claim, database migration, new package, or unrelated refactor.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel after its stated prerequisites because it changes different files.
- **[Story]**: Maps the task to a user story from `spec.md`.
- Every implementation task names its exact target file or files.

---

## Phase 1: Setup and Baseline

**Purpose**: Establish the existing behavior and protect the targeted change boundary.

- [x] T001 Run the existing community-access, community-authentication, invitation, activation, profile, teacher, grade/class, student-license, and student-viewing test filters from `SprintLabs.Tests/SprintLabs.Tests.csproj`; record any pre-existing failures before editing and preserve the unrelated local change in `API/appsettings.Development.json`

---

## Phase 2: Foundational Trusted Staff Resolver

**Purpose**: Provide the trusted database-derived tenant context required by staff routes and legacy-conflict checks.

**⚠️ CRITICAL**: Complete this phase before migrating `CommunitiesController` routes.

- [x] T002 Add failing resolver matrix tests in `SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceTests.cs` for one Active Owner/Teacher in an Active Community, zero membership, Pending-only, Removed-only, Student-only, suspended Community, Active plus Removed/Student, every Owner/Teacher Pending/Active cross-community conflict, and insertion-order independence
- [x] T003 Replace the role-specific lookup with `ResolveCurrentStaffCommunityId(long userId, CancellationToken cancellationToken = default)` in `Domain/Services/ICommunityAccessService.cs`, retaining the existing access/role authorization methods
- [x] T004 Implement the combined Owner/Teacher resolver in `Infrastructure/Services/CommunityAccessService.cs`: detect distinct Pending/Active staff communities before eligibility filtering, return only one Active membership in one Active Community, and return null for zero or ambiguous state without `First`, ordering, or tie-breaking
- [x] T005 Run the focused resolver tests in `SprintLabs.Tests/Features/CommunityAccessFoundation/CommunityAccessServiceTests.cs` and confirm all success/fail-closed cases pass

**Checkpoint**: Tenant context can be resolved from trusted `userId` and current database membership without granting endpoint authorization.

---

## Phase 3: User Story 1 — Use Staff APIs Without Selecting a Tenant (Priority: P1) 🎯 MVP

**Goal**: All 17 Owner/Teacher API operations derive CommunityId server-side while existing MediatR requests continue receiving an internal CommunityId.

**Independent Test**: Authenticate an Owner or Teacher with exactly one valid staff Community, call each migrated route without CommunityId, and verify the dispatched command/query is scoped to that Community; zero or conflicting membership dispatches no community operation.

### Tests for User Story 1

- [x] T006 [P] [US1] Add failing reflection and dispatch tests for all tenantless staff route templates, `GET/PATCH /api/v1/Communities/me`, absence of client CommunityId parameters/body fields, resolver failure, and server-populated MediatR CommunityId in `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs`
- [x] T007 [P] [US1] Update failing invitation handler tests for an internally supplied CommunityId and retained Active Owner authorization in `SprintLabs.Tests/Features/OwnerTeacherManagement/InviteTeacherCommandHandlerTests.cs`, removing reliance on `GetSingleActiveCommunityIdForRole`

### Implementation for User Story 1

- [x] T008 [US1] Add server-owned `CommunityId` to `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommand.cs` and update `Application/Features/Communities/Teachers/InviteTeacher/InviteTeacherCommandHandler.cs` to validate Active Owner membership for that CommunityId before calling `ITeacherInvitationService.IssueAsync`
- [x] T009 [US1] Inject `ICommunityAccessService` into `API/Controllers/CommunitiesController.cs`, add one controlled current-staff resolver helper, add `GET/PATCH me`, remove CommunityId from the remaining teacher/grade/class/student-license/student staff route templates and action inputs, populate existing command/query CommunityId internally, and leave `GET {communityId:long}` intact
- [x] T010 [US1] Run `CommunitiesControllerRouteContractTests`, `InviteTeacherCommandHandlerTests`, and `CommunityAccessServiceTests` from `SprintLabs.Tests/SprintLabs.Tests.csproj` and confirm malformed identity, zero membership, and multiple-membership cases fail before MediatR dispatch

**Checkpoint**: Owner/Teacher clients cannot select a tenant, and every migrated operation still enters its existing CQRS handler with a server-derived CommunityId.

---

## Phase 4: User Story 2 — Preserve Existing Staff Permissions (Priority: P1)

**Goal**: Tenant resolution identifies the Community only; Owner-only, Owner-or-Teacher, user-status, and resource-ownership authorization remain enforced by existing handlers.

**Independent Test**: Run the established handler suites as Owner, Teacher, Student, Pending, Removed, suspended, unrelated, and PlatformAdmin-only identities and verify the permission matrix and cross-tenant resource rejection are unchanged.

### Tests and Authorization Preservation for User Story 2

- [x] T011 [P] [US2] Extend only missing role-regression assertions in `SprintLabs.Tests/Features/OwnerCommunityProfile/GetCommunityProfileQueryHandlerTests.cs`, `SprintLabs.Tests/Features/OwnerCommunityProfile/UpdateCommunityProfileCommandHandlerTests.cs`, `SprintLabs.Tests/Features/OwnerTeacherManagement/ListTeachersQueryHandlerTests.cs`, and `SprintLabs.Tests/Features/OwnerTeacherManagement/RemoveTeacherCommandHandlerTests.cs` so Teacher remains denied from Owner-only operations and Student/PlatformAdmin-only state grants no staff-route bypass
- [x] T012 [P] [US2] Extend only missing Owner-or-Teacher and foreign-resource assertions in `SprintLabs.Tests/Features/CommunityGradesClasses/CreateClassCommandHandlerTests.cs`, `SprintLabs.Tests/Features/CommunityGradesClasses/UpdateClassCommandHandlerTests.cs`, `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/AddStudentLicenseCommandHandlerTests.cs`, `SprintLabs.Tests/Features/OwnerStudentLicenseManagement/UpdateStudentLicenseCommandHandlerTests.cs`, and `SprintLabs.Tests/Features/CommunityStudentViewing/GetStudentDetailQueryHandlerTests.cs`
- [x] T013 [US2] Run the profile, teacher-management, grade/class, student-license, and student-viewing suites in `SprintLabs.Tests/SprintLabs.Tests.csproj`; fix only regressions caused by server-populated CommunityId and do not remove authorization or CommunityId ownership checks from handlers under `Application/Features/Communities/`

**Checkpoint**: The migrated routes preserve every established role and tenant-isolation decision.

---

## Phase 5: User Story 3 — Prevent Conflicting Staff Memberships (Priority: P1)

**Goal**: Every staff membership writer prevents Pending/Active Owner or Teacher memberships from spanning multiple Communities, including concurrent attempts, while Removed history remains reusable.

**Independent Test**: Exercise invitation issue/completion, pending Teacher activation, restoration, and Platform Admin owner assignment with all Owner/Teacher and Pending/Active combinations across two Communities; exactly one operation may establish a current staff Community and rejected operations leave no partial state.

### Tests for User Story 3

- [x] T014 [US3] Add failing `TeacherInvitationService.IssueAsync` theories for Owner→Owner, Teacher→Teacher, Owner→Teacher, and Teacher→Owner conflicts across every Pending/Active combination, plus Removed reassignment, same-Community reissue, seat accounting, and no-partial-mutation assertions in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationServiceTests.cs`
- [x] T015 [US3] Add failing `TeacherInvitationService.CompleteAsync` tests that introduce a cross-role current membership after issue and verify password/profile/email confirmation, target membership, invitation state, refresh revocation, and capacity remain unchanged on conflict in `SprintLabs.Tests/Features/InviteOnlyTeacherAuthentication/TeacherInvitationServiceTests.cs`
- [x] T016 [P] [US3] Add failing all-or-none pending Teacher activation tests for one valid Community, mixed Owner/Teacher conflicts, multiple eligible Communities, Removed non-conflict, and unchanged Student activation in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/FirebaseLoginActivationTests.cs`
- [x] T017 [P] [US3] Add failing Platform Admin owner-assignment tests using the real atomic membership service path for cross-role/status conflicts, Removed other-Community reassignment, Removed target conversion, idempotent Owner assignment, incompatible current same-Community role denial, and no partial mutation in `SprintLabs.Tests/Features/CurrentCommunityResolution/AssignOwnerCommandHandlerTests.cs`
- [x] T018 [P] [US3] Add opt-in real-MySQL race tests with separate DbContexts for invitation versus owner assignment and two cross-Community owner assignments, using `SprintLabs.Tests/Fixtures/MysqlDatabaseFixture.cs`, in `SprintLabs.Tests/Features/CurrentCommunityResolution/StaffMembershipConcurrencyTests.cs`

### Implementation for User Story 3

- [x] T019 [US3] Add the internal User-row lock and combined Pending/Active Owner/Teacher conflict predicate in `Infrastructure/Services/Common/StaffCommunityMembershipIntegrity.cs`, using `Infrastructure/DataAccess/ApplicationDbContext.cs`, `SELECT ... FOR UPDATE` for relational MySQL, a non-relational test-provider fallback, and the lock order User → current memberships → target membership → related rows
- [x] T020 [US3] Update `TeacherInvitationService.IssueAsync` in `Infrastructure/Services/TeacherInvitationService.cs` to lock the invited User and reject any other-Community current Owner/Teacher membership before identity, membership, invitation, or capacity mutations, while preserving compatible same-Community reissue and Removed reuse
- [x] T021 [US3] Update `TeacherInvitationService.CompleteAsync` in `Infrastructure/Services/TeacherInvitationService.cs` to re-lock and recheck the combined staff set before password/profile/membership/invitation changes, preserving replay protection and rolling back every conflict side effect
- [x] T022 [P] [US3] Make only pending Teacher activation transactional and all-or-none in `Infrastructure/Services/CommunityLoginActivationService.cs`, using the shared User lock/check while preserving the existing Student-license activation path and single-pending legacy behavior
- [x] T023 [P] [US3] Add `IStaffCommunityMembershipService.AssignOwnerAsync` in `Domain/Services/IStaffCommunityMembershipService.cs` and implement its narrow transactional create/restore/idempotency/conflict behavior in `Infrastructure/Services/StaffCommunityMembershipService.cs`
- [x] T024 [US3] Register the owner-membership service in `Infrastructure/ServiceConfig.cs` and update `Application/Features/Admin/Communities/AssignOwner/AssignOwnerCommandHandler.cs` to retain Platform Admin/input/community/user validation while delegating only the atomic membership mutation and mapping controlled conflicts through existing conventions
- [x] T025 [US3] Run the non-MySQL invitation, activation, and owner-assignment focused tests in `SprintLabs.Tests/SprintLabs.Tests.csproj` and confirm every rejected path leaves identity, membership, invitation, refresh, and capacity state unchanged
- [x] T026 [US3] When `SPRINTLABS_MYSQL_TEST_CONNECTION` points to an approved dedicated test database, run `SprintLabs.Tests/Features/CurrentCommunityResolution/StaffMembershipConcurrencyTests.cs` and verify at most one distinct Pending/Active staff Community remains; otherwise record the suite as safely skipped

**Checkpoint**: All known creation, invitation, completion, activation, restoration, and owner-assignment paths preserve the combined Owner/Teacher invariant without a database migration.

---

## Phase 6: User Story 4 — Fail Closed on Legacy Conflicts (Priority: P2)

**Goal**: Current-community resolution, Community password login, and refresh reject legacy cross-Community staff conflicts without choosing a tenant or issuing/rotating credentials.

**Independent Test**: Seed Active+Active and Active+Pending mixed-role memberships across Communities, including a PlatformAdmin user, and verify resolver/login/refresh return no selected tenant or usable replacement credentials; Removed and Student rows do not create conflicts.

### Tests for User Story 4

- [x] T027 [US4] Add failing Community login tests for all mixed-role Active/Pending conflicts, an Active membership in a suspended second Community, PlatformAdmin conflict before token issue, Pending-only/zero-current behavior, and Removed/Student non-conflicts in `SprintLabs.Tests/Features/CommunityAuthentication/CommunityLoginCommandHandlerTests.cs`
- [x] T028 [P] [US4] Add persisted refresh tests for the same conflict matrix, PlatformAdmin conflict, Removed/Student non-conflicts, valid CommunityAdmin/Teacher account-type reconstruction, and zero revoke/create calls on failure in `SprintLabs.Tests/Features/CurrentCommunityResolution/RefreshTokenServiceTests.cs`
- [x] T029 [P] [US4] Extend JWT regression assertions so Teacher and PlatformAdmin access tokens contain neither CommunityId nor community-role claims in `SprintLabs.Tests/Features/TeacherEmailAuthentication/AccessTokenServiceTests.cs`

### Implementation for User Story 4

- [x] T030 [P] [US4] Update `Application/Features/Accounts/CommunityAuthentication/CommunityLogin/CommunityLoginCommandHandler.cs` to detect distinct Pending/Active Owner/Teacher Communities before the PlatformAdmin branch, issue no tokens on conflict, and preserve existing zero/pending/suspended and valid Admin/staff behavior
- [x] T031 [P] [US4] Update `Infrastructure/Services/RefreshTokenService.cs` to re-evaluate the combined current staff set before revoke/rotation, return no rotation for conflicts including PlatformAdmin, preserve valid Admin/staff reconstruction, and never select a membership with `First` or add CommunityId to token results
- [x] T032 [US4] Run `CommunityLoginCommandHandlerTests`, `RefreshTokenServiceTests`, `AccessTokenServiceTests`, and existing refresh/logout regressions from `SprintLabs.Tests/SprintLabs.Tests.csproj`

**Checkpoint**: Legacy conflicts remain stored but cannot select a tenant, authenticate a staff session, or rotate credentials.

---

## Phase 7: User Story 5 — Migrate Frontend Routes Without Breaking Student or Admin Flows (Priority: P2)

**Goal**: Publish and verify the exact staff migration while retaining intentional Student profile and Platform Admin multi-Community contracts.

**Independent Test**: Reflection tests show exactly 17 Community staff operations without request-side CommunityId, retained Student-compatible `GET /Communities/{communityId:long}`, and unchanged explicit CommunityId parameters on both Admin routes; documentation matches those contracts.

### Tests and Compatibility for User Story 5

- [x] T033 [US5] Extend `SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs` to assert the old numeric staff templates are absent, `GET /api/v1/Communities/{communityId:long}` still dispatches the existing Student-compatible profile query, and `API/Controllers/AdminCommunitiesController.cs` still exposes explicit CommunityId on owner assignment and license update without granting PlatformAdmin-only access to `CommunitiesController`

### Documentation for User Story 5

- [x] T034 [P] [US5] Reconcile the implemented endpoint, role, body/query, response, and error contracts against `specs/018-current-community-resolution/api.md` and `specs/018-current-community-resolution/contracts/current-community-api.md`, then add concise supersession notices to affected API docs in `specs/004-owner-community-profile/api.md`, `specs/005-owner-teacher-management/api.md`, `specs/006-community-grades-classes/api.md`, `specs/007-owner-student-license-management/api.md`, `specs/009-community-student-viewing/api.md`, and `specs/015-teacher-email-authentication/api.md`
- [x] T035 [P] [US5] Reconcile the exact 17-call old→new migration, removal of stored/request CommunityId, pages/actions/forms/tables/states/permissions, Student exception, and Admin exception in `specs/018-current-community-resolution/frontend.md`, then add concise supersession notices to `specs/004-owner-community-profile/frontend.md`, `specs/005-owner-teacher-management/frontend.md`, `specs/006-community-grades-classes/frontend.md`, `specs/007-owner-student-license-management/frontend.md`, `specs/009-community-student-viewing/frontend.md`, and `specs/015-teacher-email-authentication/frontend.md`
- [x] T036 [US5] Run `CommunitiesControllerRouteContractTests` from `SprintLabs.Tests/SprintLabs.Tests.csproj` and manually compare all 17 implemented action templates/request DTOs with `specs/018-current-community-resolution/api.md` and `specs/018-current-community-resolution/frontend.md`

**Checkpoint**: Frontend consumers have one authoritative migration guide, Student profile access remains available, and Admin routes remain explicitly scoped.

---

## Phase 8: Polish and Cross-Cutting Verification

**Purpose**: Verify the complete security slice without expanding scope.

- [x] T037 Run the focused feature filter first from `SprintLabs.Tests/SprintLabs.Tests.csproj`, covering `CommunityAccessServiceTests`, `CommunitiesControllerRouteContractTests`, `InviteTeacherCommandHandlerTests`, `TeacherInvitationServiceTests`, `FirebaseLoginActivationTests`, `AssignOwnerCommandHandlerTests`, `CommunityLoginCommandHandlerTests`, `RefreshTokenServiceTests`, `AccessTokenServiceTests`, and affected authorization regressions
- [x] T038 When a safe MySQL test connection is configured, run the opt-in concurrency tests in `SprintLabs.Tests/Features/CurrentCommunityResolution/StaffMembershipConcurrencyTests.cs`; otherwise document why this provider-specific check was not run
- [x] T039 Run `dotnet build SprintLabs.sln` from the repository root and resolve only feature-related build errors or warnings
- [ ] T040 If practical in the available environment, run `dotnet test SprintLabs.sln --no-build`; report any infrastructure-dependent skips or failures separately from feature failures
- [x] T041 Confirm `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` and `Infrastructure/DataAccess/ApplicationDbContext.cs` have no unintended model change, no migration was added under `Infrastructure/Migrations/`, `Infrastructure/Services/AccessTokenService.cs` has no CommunityId claim, `API/Controllers/AdminCommunitiesController.cs` retains its explicit routes, and implementation matches `specs/018-current-community-resolution/quickstart.md` without unrelated refactors

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately.
- **Phase 2 — Resolver foundation**: Depends on T001 and blocks the tenantless controller work.
- **US1 (Phase 3)**: Depends on Phase 2; delivers the HTTP boundary and internal CommunityId flow.
- **US2 (Phase 4)**: Depends on US1 route adaptation so existing authorization can be exercised through the new boundary.
- **US3 (Phase 5)**: May begin after Phase 2 and can proceed in parallel with US1/US2 once its failing tests exist; it must complete before production release.
- **US4 (Phase 6)**: Its tests may begin after Phase 2, but final authentication behavior must be integrated with US3's combined invariant semantics.
- **US5 (Phase 7)**: Depends on US1 and US2 route/authorization results; compatibility tests can be prepared earlier.
- **Polish (Phase 8)**: Depends on every story selected for delivery.

### User Story Completion Order

```text
Setup → Resolver foundation → US1 → US2 → US5
                         └──→ US3 → US4 ──┘
```

- **US1** is the technical MVP: trusted current-community staff routes.
- **US2** proves tenant resolution did not weaken authorization.
- **US3** prevents new conflicting data and is a security release dependency.
- **US4** keeps legacy conflicting data fail-closed and is a security release dependency.
- **US5** completes compatibility verification and frontend adoption documentation.

### Within Each Story

- Write/update focused tests first and verify the new assertions fail for the intended missing behavior.
- Implement the smallest service/controller/handler change needed for those tests.
- Run the story's focused tests before proceeding.
- Keep same-file tasks sequential: T014 before T015, T020 before T021, and T006 before T033.
- Run the full solution only after focused failures are resolved.

## Parallel Opportunities

- T006 and T007 can run in parallel after the resolver foundation.
- T011 and T012 can run in parallel because they cover separate handler suites.
- T016, T017, and T018 can be authored in parallel with T014/T015.
- After T019, activation work T022 and owner-service work T023 can run in parallel with invitation work T020/T021.
- T027, T028, and T029 can be authored in parallel; T030 and T031 can then proceed in parallel.
- T034 and T035 can run in parallel after route contracts stabilize.

## Parallel Example: User Story 3

```text
Task T014/T015: Add invitation issue/completion invariant tests.
Task T016: Add pending Teacher activation invariant tests.
Task T017: Add owner-assignment invariant tests.
Task T018: Add opt-in MySQL concurrency tests.

After T019 establishes the common lock/check helper:
Task T020/T021: Update invitation issue/completion.
Task T022: Update pending Teacher activation.
Task T023: Implement atomic owner membership persistence.
```

## Parallel Example: User Story 4

```text
Task T027: Add Community login legacy-conflict tests.
Task T028: Add persisted refresh legacy-conflict tests.
Task T029: Add JWT no-community-claim regression tests.

Then implement T030 and T031 in parallel before running T032.
```

## Implementation Strategy

### Technical MVP

1. Complete Phase 1 and Phase 2.
2. Complete US1.
3. Run T010 and demonstrate that the client cannot select CommunityId.

Do not deploy the technical MVP alone. The production-safe minimum also includes US2, US3, and US4 because authorization preservation, new-conflict prevention, and legacy-conflict rejection are part of the same security boundary.

### Incremental Delivery

1. Resolver foundation → prove exact-one resolution and zero/multiple fail-closed behavior.
2. US1 + US2 → migrate the HTTP boundary while preserving handler authorization.
3. US3 → prevent all new cross-role/current-status conflicts transactionally.
4. US4 → protect login and refresh from legacy conflicting data.
5. US5 → lock compatibility and publish the frontend migration.
6. Phase 8 → run focused tests, optional MySQL concurrency, build, then the full suite when practical.

## Notes

- CommunityId may remain in response DTOs and internal commands/queries; it must not come from the Owner/Teacher HTTP client.
- Pending and Active Owner/Teacher memberships count for conflicts; only Active membership in an Active Community resolves access.
- Removed and Student memberships do not count toward the staff invariant.
- `GET /api/v1/Communities/{communityId}` remains for existing Active Owner, Teacher, and Student profile access.
- Platform Admin routes continue accepting explicit CommunityId and do not bypass `CommunitiesController` membership authorization.
- No task authorizes a schema constraint, migration, JWT CommunityId, generic tenant middleware, global MediatR behavior, custom repository, package, or legacy-data rewrite.
