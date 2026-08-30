# Research: Trusted Current-Community Resolution

**Feature**: [spec.md](./spec.md)  
**Date**: 2026-08-30

## Decision 1: Resolve at the HTTP-to-MediatR boundary

**Decision**: Add `ResolveCurrentStaffCommunityId(long userId, CancellationToken)` to `ICommunityAccessService`. `CommunitiesController` calls it after reading the signed `userId` and copies the result into existing internal MediatR commands/queries.

**Rationale**:

- `CommunityId` becomes server-owned request context rather than HTTP client input.
- Existing commands/queries already carry `CommunityId`, and existing handlers recheck suspended-user state, role, and resource community ownership.
- One controller helper keeps the repeated adaptation small while avoiding changes across every Application handler.
- API already depends on Domain types and the access service is already registered as scoped.

**Alternatives considered**:

- Resolve independently in every handler: rejected because it duplicates the same lookup across roughly fourteen slices and changes far more files.
- Add a MediatR resolver query and perform two sends per request: rejected as ceremony without additional business isolation.
- Add middleware, an action filter, or a global MediatR behavior: rejected as a new tenant framework and explicitly outside scope.
- Put CommunityId in the JWT: rejected because membership is mutable and the database must remain authoritative.

## Decision 2: Resolve from the complete current staff set

**Decision**: Define the resolver input set as Owner or Teacher membership with Pending or Active status. Count distinct community IDs first; return a community only when the set contains exactly one community and its staff membership and Community are both Active.

**Rationale**:

- Counting only Active rows would incorrectly accept Active Community A while ignoring Pending Community B.
- Filtering suspended communities before conflict detection would incorrectly hide current membership conflicts.
- Removed rows are historical and Student rows belong to a separate access model.
- Returning `null` for every ambiguous/absent state allows the controller to use one safe 403 response without revealing membership details.

**Alternatives considered**:

- Choose the first, lowest ID, latest, or Owner-preferred membership: rejected because it lets inconsistent data select a tenant arbitrarily.
- Resolve only the role required by the endpoint: rejected because Owner/Teacher cross-role conflicts are part of the invariant and role authorization remains the handler's job.

## Decision 3: Serialize membership writers on the existing User row

**Decision**: Every operation that can make an Owner/Teacher membership Pending or Active starts or joins a short transaction, locks the target existing User row with `SELECT ... FOR UPDATE`, checks Owner+Teacher Pending+Active memberships while holding that lock, and mutates membership-related rows only after the check passes.

**Rationale**:

- A canonical User row exists for every membership and supplies one common lock key across invitations, activation, and owner assignment.
- Two cross-community writes for one user queue on the same row; the loser observes the winner's committed membership and returns a controlled conflict.
- Locking a concrete row is more predictable than relying on empty-range gap locking across different role/status index ranges.
- A consistent lock order—User, current memberships, target membership, community/license/invitation—reduces deadlock risk.
- Existing Teacher invitation operations already use Serializable transactions; the new owner and activation transactions remain similarly narrow.

**Alternatives considered**:

- Application-only precheck: rejected because two requests can both observe no membership and commit invalid rows.
- Serializable membership query without a canonical row lock: rejected because range/gap behavior is provider/index dependent and different role predicates can lock different ranges.
- Extend `IBaseRepository` with generic transaction/lock APIs: rejected as a repository-wide abstraction for one special persistence rule.
- MySQL named locks: rejected because connection ownership/release semantics add provider-specific operational complexity.

**Primary references**:

- [MySQL InnoDB locking](https://dev.mysql.com/doc/refman/8.0/en/innodb-locking.html)
- [Locks set by InnoDB statements](https://dev.mysql.com/doc/refman/8.0/en/innodb-locks-set.html)
- [MySQL transaction isolation levels](https://dev.mysql.com/doc/refman/8.0/en/innodb-transaction-isolation-levels.html)
- [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)

## Decision 4: Add a narrow owner-membership persistence service

**Decision**: Introduce `IStaffCommunityMembershipService.AssignOwnerAsync` in Domain with an Infrastructure implementation. `AssignOwnerCommandHandler` keeps Platform Admin/input/community/user validation and delegates only the atomic membership mutation.

**Rationale**:

- Application cannot reference `ApplicationDbContext` or issue MySQL row locks without violating dependency direction.
- Direct base-repository mutation cannot express the required atomic lock/check/write boundary.
- The service owns one special persistence operation and does not become a generic tenant or membership framework.

**Alternatives considered**:

- Put owner assignment into `ICommunityAccessService`: rejected because access resolution should remain read-oriented.
- Add owner assignment to `ITeacherInvitationService`: rejected because the existing Admin action performs direct Active assignment, not invitation lifecycle behavior.
- Move the entire Admin handler into Infrastructure: rejected because CQRS validation/response mapping belongs in Application.

## Decision 5: Share only an internal Infrastructure lock/check helper

**Decision**: Add an internal `StaffCommunityMembershipIntegrity` helper that locks a User and checks for another current staff community inside an existing transaction. Use it from invitation issue/completion, activation, and the owner-membership service.

**Rationale**:

- The critical role/status predicate and lock ordering must not drift across writers.
- The helper has no public API, no registration, and no tenant-context responsibility.
- Callers retain their operation-specific validation, target-role compatibility, error code, and side effects.

**Alternatives considered**:

- Duplicate raw `FOR UPDATE` and invariant queries in three services: rejected because a future role/status mismatch would reopen the security gap.
- Add a new public generic guard service: rejected as more architecture than the story needs.

## Decision 6: Do not add a database constraint in this rollout

**Decision**: Make no schema or migration change.

**Rationale**:

- A plain unique `UserId` index is invalid because one user may have Student rows and Removed history across communities.
- MySQL can technically express a safe conditional key with a nullable generated column:

  ```sql
  CASE
    WHEN Role IN (1, 2) AND Status IN (1, 2) THEN UserId
    ELSE NULL
  END
  ```

  A unique index on that value would restrict current Owner/Teacher rows while allowing multiple Student/Removed NULL values.
- Existing legacy conflicts would cause that unique index creation to fail before the fail-closed application changes could deploy.
- Automatic conflict selection or cleanup is explicitly outside scope.
- Transactional prevention plus fail-closed reads can deploy without rewriting existing data.

**Alternatives considered**:

- Conditional generated-column unique index now: deferred to a later remediation/hardening feature after legacy conflicts are audited and resolved explicitly.
- Plain unique UserId: rejected because it breaks Student and Removed behavior.
- Unique `(UserId, Role, Status)`: rejected because it still permits Owner/Teacher cross-role and Pending/Active combinations in different communities.

**Primary references**:

- [MySQL CREATE INDEX, unique indexes, and NULL values](https://dev.mysql.com/doc/refman/8.0/en/create-index.html)
- [MySQL generated columns](https://dev.mysql.com/doc/refman/8.0/en/create-table-generated-columns.html)

## Decision 7: Preserve operation-specific controlled errors

**Decision**:

- Resolver failure at Communities routes: existing 403 `Failure` / `InvalidAccessToken`.
- Teacher invitation/completion/activation other-community conflict: existing 409 `TeacherAlreadyBelongsToAnotherCommunity`.
- Owner assignment conflict: existing 409 `Failure` / `ExistingRecord` convention.
- Community login multi-community legacy conflict: existing 409 `Failure` / `InvalidAccessToken`, with zero token calls.
- Refresh legacy conflict: return no rotation so the existing handler emits its generic invalid-refresh response.

**Rationale**: This keeps existing frontend handling stable and avoids a new shared error merely to rename one invariant. Detailed membership state is not exposed.

**Alternatives considered**:

- Add `StaffAlreadyBelongsToAnotherCommunity`: rejected unless implementation discovers that an existing documented error cannot represent an Owner path safely.
- Return the conflicting community: rejected as unnecessary tenant information disclosure.

## Decision 8: Keep Platform Admin and Student compatibility explicit

**Decision**:

- Retain `GET /api/v1/Communities/{communityId}` for its existing Active Owner, Teacher, and Student profile access.
- Add `GET /api/v1/Communities/me` for resolved staff profile access and move Owner update to `PATCH /me`.
- Leave `AdminCommunitiesController` route parameters unchanged.
- Platform Admin status does not resolve or authorize a Communities staff route.
- Community login/refresh detect cross-community current staff conflicts before Platform Admin bypass, but a Platform Admin with zero or one current staff community retains admin authentication behavior.

**Rationale**: These are separate intentional access models: Students need explicit profile lookup, Platform Admin manages multiple communities, and staff self-service operates on one derived current tenant.

**Alternatives considered**:

- Replace the legacy GET entirely: rejected because it breaks Student access.
- Derive Admin tenant context: rejected because Admin operations intentionally select among communities.

## Decision 9: Use focused tests plus one opt-in MySQL race suite

**Decision**: Use EF InMemory for resolver and behavior matrices, reflection/controller tests for request contracts, mocked repository tests for no-token/no-rotation outcomes, and the existing safe MySQL fixture for separate-connection race verification.

**Rationale**:

- InMemory tests are fast and sufficient for deterministic business decisions.
- They cannot prove `FOR UPDATE` serialization or MySQL transaction behavior.
- The repository already has `MysqlDatabaseFixture`, validates a dedicated test database name, and uses the configured Pomelo provider.
- Existing handler suites already cover most role and foreign-resource tenant checks, so they should remain regressions rather than be duplicated.

**Alternatives considered**:

- Build a new integration-test framework: rejected as unnecessary.
- Rely only on InMemory concurrency: rejected because it does not implement MySQL row locking.
- Expand all affected handler suites: rejected because the handlers retain their existing CommunityId authorization and resource scoping.
