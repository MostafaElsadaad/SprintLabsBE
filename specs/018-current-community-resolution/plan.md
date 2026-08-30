# Implementation Plan: Trusted Current-Community Resolution

**Branch**: `feature/staff-community-context` | **Date**: 2026-08-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/018-current-community-resolution/spec.md`

## Summary

Move Community Owner and Teacher tenant selection from client-controlled route parameters to a trusted request-time lookup based on the signed `userId` and persisted `CommunityUser` state. Add one combined Owner/Teacher resolver to the existing community-access service, call it at the `CommunitiesController` HTTP-to-MediatR boundary, and continue populating the existing internal `CommunityId` properties so current handlers keep their role and tenant checks unchanged.

Close the membership-integrity gaps in invitation issue/completion, provider-login activation, owner assignment, community login, and refresh. Every staff-membership writer will evaluate Owner and Teacher together, treat Pending and Active as current, ignore Removed and Student rows, and serialize mutation on the existing User row before checking or changing membership. This feature adds no community JWT claim, generic tenant framework, database constraint, package, or unrelated refactor.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core Web API and API versioning, MediatR, EF Core 8.0.8, Pomelo.EntityFrameworkCore.MySql 8.0.2, ASP.NET Core Identity, JWT bearer authentication

**Storage**: Existing MySQL 8 `Users`, `Communities`, `CommunityUsers`, `CommunityLicenses`, and `TeacherInvitations`; no table, column, relationship, index, or migration change

**Testing**: xUnit, FluentAssertions, Moq, EF Core InMemory for focused behavior tests, and the existing opt-in `MysqlDatabaseFixture` for real row-lock concurrency verification

**Target Platform**: Versioned ASP.NET Core backend service

**Project Type**: Layered web service (`API`, `Application`, `Domain`, `Infrastructure`, `Shared`)

**Performance Goals**: Add one indexed membership lookup per affected staff request; preserve existing handler query counts and avoid extra network calls or credential refreshes

**Constraints**: Trusted signed `userId`; database membership remains authoritative; Owner and Teacher are staff; Pending and Active are current; only Active membership in an Active community resolves; Removed/Student ignored by staff invariant; no client `communityId`; no JWT community claim; handlers retain authorization; legacy conflicts fail closed; no schema remediation

**Scale/Scope**: Seventeen staff route contracts, one access-service resolver, four staff-membership write paths, two authentication/session paths, consolidated API/frontend documentation, and focused regression coverage

**Planning Baseline**: `dotnet build SprintLabs.sln` succeeds with 17 pre-existing warnings; `dotnet test SprintLabs.sln --no-build` passes 261 tests with 0 failures and 0 skips

## Constitution Check

### Pre-design gate

| Gate | Result | Evidence |
|---|---|---|
| Vertical slice delivery | PASS | Plan covers HTTP contracts, Application dispatch, Domain service boundaries, Infrastructure persistence, focused tests, and frontend-facing documentation. |
| Existing architecture wins | PASS | Controller remains an adapter, MediatR commands/queries remain internal, handlers retain authorization, service contracts remain in Domain, and implementations remain in Infrastructure. |
| SaaS data isolation | PASS | Tenant context is derived from signed identity and live membership; resource queries remain scoped by the internally resolved community. |
| Minimum useful implementation | PASS | One resolver method, one narrow owner-membership persistence service, and one internal lock/check helper replace no broader architecture. |
| Quality gates | PASS | Baseline build and 261 tests pass; plan adds focused unit/contract tests and an opt-in MySQL race test. |
| Documentation | PASS | Plan produces `api.md`, `frontend.md`, an API contract, data model, research record, and quickstart in feature 018. |
| Persistence safety | PASS | No schema change or legacy-data rewrite; mutation paths use short transactions and existing rows. |

### Post-design gate

PASS. Phase 1 introduces no project reference, package, generic tenant middleware, global MediatR behavior, custom repository, duplicate membership store, JWT community claim, or database migration. The only new Domain/Infrastructure service is justified by the atomic owner-assignment transaction that Application cannot safely implement through `IBaseRepository` alone.

## Current Implementation Map

| Concern | Current implementation | Planned disposition |
|---|---|---|
| HTTP tenant selection | `CommunitiesController` reads `communityId` from most staff route parameters and copies it into MediatR messages | Remove staff route parameters; resolve once through `ICommunityAccessService`; continue populating internal `CommunityId`. |
| Community profile read | `GET Communities/{communityId}` allows Active Owner, Teacher, or Student | Retain unchanged; add separate staff-only `GET Communities/me`. |
| Profile update | `PATCH Communities/{communityId}` sends `UpdateCommunityProfileCommand` | Move to `PATCH Communities/me`; command and handler keep internal `CommunityId` and Owner check. |
| Teacher invitation | Route already omits community ID, but `InviteTeacherCommandHandler` uses a role-specific Owner resolver | Add internal `CommunityId` to the command, resolve in controller, and retain Owner authorization in handler/service. |
| Other staff routes | Teacher list/remove, grades/classes, licenses, and students receive route `communityId` | Remove route parameter and use resolved ID; keep messages and handlers unchanged. |
| Current resolver | `GetSingleActiveCommunityIdForRole` checks one role and Active memberships only | Replace with combined `ResolveCurrentStaffCommunityId(userId, cancellationToken)` using Owner+Teacher and Pending+Active conflict detection. |
| Invitation issue/completion | Queries current memberships only for the requested role | Lock User row and evaluate Owner+Teacher together before any identity, membership, invitation, or capacity mutation. |
| Provider-login teacher activation | Iterates matching Pending Teacher rows and may activate multiple communities | Lock User row, evaluate the complete current staff set, and activate at most one eligible pending Teacher membership atomically. |
| Owner assignment | Handler directly creates/restores/converts membership through base repository with no transaction | Delegate atomic membership write to a narrow Infrastructure service; reject other-community current staff and incompatible current same-community roles. |
| Community login | Detects multiple eligible Active memberships only after an early Platform Admin return | Detect cross-community current staff conflicts before account-type handling; then preserve normal Admin or single-active-staff eligibility. |
| Refresh | Non-admin path checks eligible Active memberships and Pending rows; Platform Admin bypasses membership conflicts | Detect cross-community current staff conflicts for every account before rotation/account-type selection; keep other rotation behavior unchanged. |
| JWT | Access token contains trusted `userId` and informational `accountType`, no community claim | Preserve unchanged and add a narrow regression assertion for Teacher tokens. |
| Database model | Unique `(CommunityId, UserId)` plus non-unique membership lookup indexes | Preserve unchanged; do not add a plain or conditional unique UserId index in this rollout. |

## Project Structure

### Documentation (this feature)

```text
specs/018-current-community-resolution/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- current-community-api.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                         # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
API/Controllers/
|-- CommunitiesController.cs                         # resolve staff context; migrate staff routes
`-- AdminCommunitiesController.cs                    # unchanged explicit communityId contract

Domain/Services/
|-- ICommunityAccessService.cs                       # replace role-specific resolver
`-- IStaffCommunityMembershipService.cs              # new narrow atomic Owner-assignment boundary

Application/Features/
|-- Accounts/CommunityAuthentication/CommunityLogin/
|   `-- CommunityLoginCommandHandler.cs              # combined current-staff conflict handling
|-- Admin/Communities/AssignOwner/
|   `-- AssignOwnerCommandHandler.cs                 # delegate atomic membership mutation
`-- Communities/Teachers/InviteTeacher/
    |-- InviteTeacherCommand.cs                      # internal CommunityId
    `-- InviteTeacherCommandHandler.cs               # retain Owner role authorization

Infrastructure/
|-- ServiceConfig.cs                                 # register owner-membership service
`-- Services/
    |-- CommunityAccessService.cs                    # trusted combined resolver
    |-- TeacherInvitationService.cs                  # locked combined issue/completion checks
    |-- CommunityLoginActivationService.cs           # atomic pending Teacher activation
    |-- StaffCommunityMembershipService.cs           # atomic Owner membership assignment
    |-- RefreshTokenService.cs                       # legacy conflict fail-closed logic
    `-- Common/
        `-- StaffCommunityMembershipIntegrity.cs     # internal User lock/current-staff check helper

SprintLabs.Tests/Features/
|-- CommunityAccessFoundation/
|   `-- CommunityAccessServiceTests.cs               # resolver matrix
|-- CommunityAuthentication/
|   `-- CommunityLoginCommandHandlerTests.cs         # legacy conflict matrix
|-- InviteOnlyTeacherAuthentication/
|   |-- InvitationApiContractTests.cs                # remove obsolete command assertion
|   `-- TeacherInvitationServiceTests.cs             # issue/completion cross-role checks
|-- FirebasePlayerAuthentication/
|   `-- FirebaseLoginActivationTests.cs              # activation all-or-none checks
|-- TeacherEmailAuthentication/
|   `-- AccessTokenServiceTests.cs                   # no community claim regression
`-- CurrentCommunityResolution/
    |-- CommunitiesControllerRouteContractTests.cs   # 17 route contracts + compatibility
    |-- AssignOwnerCommandHandlerTests.cs             # owner invariant behavior
    |-- RefreshTokenServiceTests.cs                   # persisted refresh conflict behavior
    `-- StaffMembershipConcurrencyTests.cs            # opt-in real MySQL race verification
```

No file under `Infrastructure/Migrations/`, `ApplicationDbContext.OnModelCreating`, or `ApplicationDbContextModelSnapshot` changes.

**Structure Decision**: Resolve at the HTTP-to-MediatR adapter boundary because the value is server-owned request context, not endpoint business input. Keep the existing MediatR messages/handlers for every route except enriching `InviteTeacherCommand` with the same internal `CommunityId` already used elsewhere. Use a narrow Domain/Infrastructure service only for owner assignment because a cross-path transaction and row lock cannot be expressed safely through Application's current base-repository surface.

## Design Interfaces

### Community resolver

```csharp
public interface ICommunityAccessService
{
    Task<bool> CanAccessCommunity(long userId, long communityId);
    Task<bool> HasCommunityRole(long userId, long communityId, IEnumerable<CommunityUserRole>? roles);
    Task<long?> ResolveCurrentStaffCommunityId(
        long userId,
        CancellationToken cancellationToken = default);
}
```

Remove `GetSingleActiveCommunityIdForRole`; its only caller is the Teacher invitation handler and its role-specific semantics are unsafe for the new invariant.

The resolver loads at most two distinct community candidates from memberships satisfying:

```text
UserId == authenticated user
Role in {Owner, Teacher}
Status in {Pending, Active}
```

It returns the sole community ID only when there is exactly one distinct current community, the matching staff membership is Active, and the Community is Active. It ignores Removed and Student rows. It returns `null` for zero, pending-only, suspended-community, or multi-community state and never orders candidates to choose one.

### Atomic owner assignment

```csharp
public interface IStaffCommunityMembershipService
{
    Task<CommunityUser> AssignOwnerAsync(
        long userId,
        long communityId,
        CancellationToken cancellationToken);
}
```

The handler remains responsible for Platform Admin authorization, input validation, community existence, user resolution, and response mapping. The Infrastructure implementation owns only the transaction, User-row lock, combined current-staff check, and target membership mutation.

### Internal transaction helper

`Infrastructure/Services/Common/StaffCommunityMembershipIntegrity.cs` remains internal and exposes only operations used inside an existing transaction:

```csharp
internal static Task LockUserAsync(
    ApplicationDbContext context,
    long userId,
    CancellationToken cancellationToken);

internal static Task<bool> HasCurrentStaffMembershipInAnotherCommunityAsync(
    ApplicationDbContext context,
    long userId,
    long targetCommunityId,
    CancellationToken cancellationToken);
```

`LockUserAsync` is a no-op for non-relational test providers and performs a tracked `SELECT ... FOR UPDATE` of the existing User row for MySQL. All writers use the lock order User -> current CommunityUsers -> target membership -> community/license/invitation rows.

## Implementation Strategy

### 1. Add the trusted resolver and keep role authorization separate

Implement `ResolveCurrentStaffCommunityId` in `CommunityAccessService` with one indexed membership query. Detect current communities before checking Active eligibility so Active A plus Pending/Active B fails rather than returning A. Do not check suspended-user state or allowed endpoint roles in this method; those remain existing handler responsibilities.

Extend `CommunityAccessServiceTests` with Owner/Teacher success; zero, Student-only, Pending-only, Removed-only, and suspended-community failure; Active plus Removed/Student success; every cross-role Pending/Active second-community conflict; and a non-selection assertion independent of insertion order.

### 2. Resolve at the Communities HTTP boundary

Inject `ICommunityAccessService` into `CommunitiesController`. Add one private helper that accepts the already parsed signed user ID, calls the resolver with `HttpContext.RequestAborted`, and throws the existing controlled HTTP 403 `Failure` / `InvalidAccessToken` when it returns null.

For each staff action:

1. Parse `userId` exactly as today; return 401 when absent/malformed.
2. Resolve the current staff community.
3. Copy the resolved ID into the existing command/query `CommunityId`.
4. Send through MediatR and preserve the existing response envelope.

Add `GET me`; move Owner update to `PATCH me`; remove `{communityId:long}` and action parameters from teacher list/remove, grades/classes, student licenses, and students. Retain `GET {communityId:long}` byte-for-byte behaviorally for Active Owner, Teacher, and Student membership. Do not modify `AdminCommunitiesController`.

Add internal `CommunityId` to `InviteTeacherCommand`; the route/request remain community-ID-free. Update the handler to validate Active Owner role for that internal community before calling `TeacherInvitationService.IssueAsync`. Keep the service's existing Owner verification as defense in depth.

### 3. Serialize invariant-sensitive membership writes

Do not rely on an empty membership-range query or plain Serializable isolation alone. Before any staff membership becomes Pending or Active, acquire an exclusive lock on the canonical existing User row, then query Owner+Teacher Pending+Active memberships for that user while the lock is held.

If any current staff membership belongs to another community, throw a controlled 409 before modifying user flags, membership, invitation state, password/email confirmation, community license counters, or related rows. Removed and Student memberships are excluded. Rollback guarantees no partial state.

Keep existing Serializable transactions in `TeacherInvitationService`; use the same short transaction in pending Teacher activation and owner assignment. Map Teacher issue/completion/activation conflicts to existing `TeacherAlreadyBelongsToAnotherCommunity`; map owner assignment to the existing controlled `Failure` / `ExistingRecord` conflict. Do not add another public error code unless implementation proves an existing documented code cannot represent the path.

### 4. Fix Teacher invitation issue and completion

In `IssueAsync`:

1. Validate community and inviter exactly as today.
2. Resolve or create the invited identity.
3. Lock its User row before updating `IsTeacherAccount`, timestamps, membership, invitation, or counters.
4. Query both Owner and Teacher roles with Pending or Active status.
5. Reject any other-community current row with the established operation-specific conflict.
6. Preserve same-community active idempotency, pending reissue/supersession, compatible Removed reuse, and teacher-seat accounting.

In `CompleteAsync`, lock the invitation's User row and rerun the same combined check before changing password, name, email confirmation, membership status, or invitation acceptance. Preserve one-time/replay behavior and refresh revocation.

Teacher invitation continues to reuse only a compatible Teacher membership; owner setup continues to reuse only a compatible Owner membership. Do not silently convert Student or the opposite staff role through invitation setup.

### 5. Make pending Teacher activation all-or-none

Inject the existing `ApplicationDbContext` into `CommunityLoginActivationService` while retaining its repository-based Student activation implementation. Wrap only `ActivateEligiblePendingTeacherMembershipsAsync` in a short transaction.

Lock the User row, load the complete current Owner/Teacher Pending/Active set, and determine eligible matching Teacher invitation candidates. If current staff communities span more than one community, throw the existing controlled conflict and activate none. If exactly one eligible pending Teacher community exists and no other current staff community conflicts, activate that membership and accept its matching invitation exactly once. Preserve the current legacy rule for a single pending Teacher membership with no invitation rows. Do not change Student-license activation.

### 6. Move atomic Owner mutation behind a narrow service

Keep `AssignOwnerCommandHandler` in its existing slice. After Platform Admin, request, community, and target user validation, call `IStaffCommunityMembershipService.AssignOwnerAsync` instead of directly writing `CommunityUser`.

The Infrastructure implementation starts the transaction, locks the User, rejects current Owner/Teacher membership in another community, and handles the target row:

- no target membership: create Active Owner;
- existing Active/Pending Owner: preserve idempotent Owner assignment;
- existing Removed Owner: restore Active Owner;
- existing Removed incompatible role: allow the explicit Platform Admin assignment to reuse the row and set Owner, preserving the current AssignOwner behavior;
- existing Active/Pending Teacher or Student: reject rather than silently overwrite current access.

Return the tracked `CommunityUser` after commit so the handler can preserve its current response DTO.

### 7. Preserve fail-closed login and refresh

In `CommunityLoginCommandHandler`, load Owner+Teacher memberships whose status is Pending or Active, independent of Community status, before the Platform Admin early return. If distinct current community IDs exceed one, log the existing non-sensitive conflict and return the current HTTP 409 controlled failure without issuing tokens.

After conflict detection:

- Platform Admin retains its membership-independent admin login when there is no cross-community staff conflict.
- Community Admin/Teacher requires exactly one current staff membership, Active membership status, and Active Community status.
- pending-only, zero-current, Removed-only, or suspended-community staff state remains the existing generic credential failure.

Apply the same current-set conflict detection in `RefreshTokenService` before revoking or replacing a refresh token. Cross-community conflict returns no rotation for every account type, including Platform Admin. Platform Admin without a cross-community conflict retains normal admin rotation; non-admin rotation requires one Active staff membership in an Active community. Removed and Student rows do not affect either path.

Do not place CommunityId in `RefreshTokenResult`, access-token claims, or refreshed JWTs.

### 8. Make no schema change

Do not add a unique `UserId` index: it would block supported Student and Removed rows. MySQL could enforce a conditional key with a nullable generated column such as `CASE WHEN Role IN (Owner,Teacher) AND Status IN (Pending,Active) THEN UserId ELSE NULL END`, and unique indexes permit multiple NULLs. This plan rejects that option because existing legacy conflicts would make the migration fail before the new fail-closed application behavior can deploy, while automatic conflict repair is explicitly out of scope.

Do not generate a migration. If implementation produces an EF model diff, stop and treat it as unintended.

### 9. Update exact API and frontend contracts

Create [api.md](./api.md), [frontend.md](./frontend.md), and [contracts/current-community-api.md](./contracts/current-community-api.md) as the authoritative feature-018 contract. Include all 17 old-to-new routes, retained request bodies/query filters, role matrix, response-envelope continuity, resolver failures, Student legacy GET, unchanged Admin routes, and removal of frontend community selection/storage for these staff workflows.

Mark older route documentation in specs 004-009 and 015 as superseded by feature 018 where it conflicts; do not rewrite unrelated behavior. Spec 016's current invitation route already matches and needs no route change.

## Transaction and Lock Boundaries

| Operation | Lock/transaction boundary | Required outcome |
|---|---|---|
| Invitation issue/reissue | Existing Serializable transaction; User `FOR UPDATE`; combined current-staff query; membership/license/invitation writes | Other-community current staff returns controlled conflict with no identity, seat, membership, or invitation mutation. |
| Invitation completion | Existing Serializable transaction; User `FOR UPDATE`; combined current-staff query; identity/password/membership/invitation writes | Conflict activates nothing and changes no password/profile/invitation; valid target completes once. |
| Pending Teacher activation | New short Serializable transaction; User `FOR UPDATE`; combined current-staff and invitation query | Multiple current communities activate none; one valid pending Teacher activates atomically. |
| Owner assignment | New narrow Infrastructure service transaction; User `FOR UPDATE`; combined current-staff query; target membership write | Parallel other-community assignment queues then conflicts; Removed reuse works; incompatible current role is not overwritten. |
| Current-community resolution | Read-only indexed query, no transaction | Exactly one Active staff membership in Active community returns; all ambiguity returns null. |
| Community login | Read-only current-set query before token issue | Cross-community legacy conflict returns 409 and zero tokens, including Platform Admin. |
| Refresh rotation | Existing token transaction plus current-set query before conditional revoke/replacement | Cross-community legacy conflict leaves submitted token unrotated and issues zero replacement credentials. |

## Automated Test Plan

1. Extend `CommunityAccessServiceTests` for the complete resolver matrix and insertion-order independence.
2. Add `CommunitiesControllerRouteContractTests` using the existing reflection style to assert every new template/action parameter, retained legacy GET, removed old staff templates, and unchanged Admin templates.
3. In controller contract tests, capture representative MediatR requests to prove signed `userId` plus resolved `CommunityId` are dispatched and client request DTOs contain no CommunityId.
4. Update `InvitationApiContractTests` to stop asserting that the internal `InviteTeacherCommand` lacks CommunityId; continue asserting the HTTP action and request lack it.
5. Extend `TeacherInvitationServiceTests` with Owner/Teacher x Pending/Active other-community theories for both Teacher issue and Owner setup; Removed reuse; same-community idempotency; completion no-mutation conflict; and current seat/invitation behavior.
6. Extend `FirebaseLoginActivationTests` with a single valid pending Teacher success, Owner/Teacher other-community conflicts, multiple eligible pending communities activating none, Removed non-conflict, and unchanged Student activation regression.
7. Add `AssignOwnerCommandHandlerTests` for Platform Admin authorization, four cross-role/status conflict classes, Removed other-community reassignment, Removed target conversion, incompatible current same-community denial, and no partial mutation.
8. Extend `CommunityLoginCommandHandlerTests` for Active+Pending, Active+Active suspended-community, every mixed-role conflict, Platform Admin conflict before token issue, Removed non-conflict, and current generic pending/zero behavior.
9. Add persisted `RefreshTokenServiceTests` for the same conflict matrix, Platform Admin conflict, Removed non-conflict, successful Community Admin/Teacher account-type reconstruction, and zero revoke/create calls on failure.
10. Extend `AccessTokenServiceTests` so Teacher and Platform Admin tokens explicitly contain neither a `communityId` nor community-role claim.
11. Preserve existing handler suites as tenant/role regression evidence: profile Owner/Teacher/Student, grades/classes foreign-grade/class rejection, student-license ownership, student detail foreign-player rejection, teacher removal, and refresh/logout behavior.
12. Add an opt-in real-MySQL `StaffMembershipConcurrencyTests` using separate DbContexts and the existing safe test-database fixture. Race invitation versus owner assignment and two cross-community assignments for one existing user; verify at most one current staff community and no losing-operation invitation/capacity side effects. EF InMemory tests do not substitute for this check.
13. Run `dotnet build SprintLabs.sln`, focused feature tests, then `dotnet test SprintLabs.sln`. When `SPRINTLABS_MYSQL_TEST_CONNECTION` targets a dedicated `sprintlabs-test*` or `compass-test*` database, run the MySQL race suite separately.

## Delivery Order

1. Add failing resolver tests; implement the combined resolver and remove the role-specific method.
2. Add route-contract/controller tests; migrate `CommunitiesController` and enrich `InviteTeacherCommand` while keeping handlers authorized.
3. Add failing cross-role invitation tests; implement the internal User lock/current-staff helper and update issue/completion.
4. Add failing activation tests; make pending Teacher activation transactional and all-or-none.
5. Add owner-assignment tests; introduce/register the narrow atomic owner-membership service and delegate from the handler.
6. Add login and persisted refresh conflict tests; implement pre-PlatformAdmin combined conflict detection.
7. Add JWT no-community regression and run existing compatibility suites.
8. Add and run the opt-in MySQL race tests against a dedicated safe database when configured.
9. Finalize `api.md`, `frontend.md`, contract, and quickstart; mark stale earlier route docs as superseded where needed.
10. Confirm no EF model diff or migration, then run full build and test gates.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Resolver returns an Active community while ignoring a Pending/Active conflict elsewhere | Count distinct current Owner/Teacher communities before checking Active/Community status; test every combination and insertion order. |
| Application-level prechecks race | Serialize every writer on the same existing User row and verify with separate MySQL connections. |
| AssignOwner cannot own Infrastructure transaction | Use one narrow Domain interface/Infrastructure implementation; do not expand `IBaseRepository` transaction semantics. |
| Invitation or activation partially mutates identity/counters before conflict | Acquire lock and run combined check before mutations; keep all related writes inside the same transaction. |
| Platform Admin bypasses conflict detection | Evaluate cross-community current staff state before Platform Admin login/refresh branches. |
| Student access is broken by route/schema changes | Retain `GET Communities/{communityId}`, ignore Student in resolver/invariant, add route and existing Student-handler regression coverage, and make no unique UserId index. |
| Old frontend calls still select tenants | Remove old staff route templates, publish exact migration table, and test controller reflection contracts. |
| Legacy conflicts block deployment | No database uniqueness migration or auto-remediation; deploy fail-closed reads and transactional write prevention first. |

## Complexity Tracking

No constitution violation. The narrow owner-membership service and internal lock helper are required for one atomic cross-path persistence rule; they do not create a generic tenant or repository framework.
