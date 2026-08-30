# Phase 0 Research: Fixed Grades, Teacher Classes, and Demo Community

## Existing Grade Schema and References

**Decision**: Add nullable `Grade.Value` and retain `Name` and `SortOrder` during this feature.

**Rationale**: The current Grade entity is community-scoped and referenced by `Class.GradeId` and `StudentLicense.GradeId`, both with restricted deletes. A nullable integer allows supported grades to gain the new contract while unsupported or ambiguous legacy rows remain intact. Retaining the old fields avoids a destructive schema rewrite and provides operator-readable remediation context.

**Alternatives considered**:

- Replacing `Name` with a required integer would make unresolved legacy values impossible to represent safely.
- A global Grade catalog would change existing community foreign keys and add an abstraction explicitly outside scope.
- Reusing `SortOrder` as the public value would blur ordering and identity semantics and could silently reinterpret custom data.

## Legacy Grade Recognition and Backfill

**Decision**: Recognize only trimmed `7`-`12` and case-insensitive exact `Grade 7`-`Grade 12`. Preserve the existing row only when one row uniquely represents a community/value pair. Insert missing supported rows. Leave custom and out-of-range rows unresolved with `Value = NULL`; abort migration before changes when duplicate-equivalent rows make recognition ambiguous.

**Rationale**: This preserves recognized Grade IDs and existing references without guessing. Duplicate-equivalent rows cannot be merged safely because both may have live references, so a fail-fast preflight leaves them untouched for explicit remediation. A nullable unresolved state separates other legacy preservation from supported new-data behavior, while every successfully migrated Community receives all six supported values.

**Alternatives considered**:

- Picking the first/minimum row would violate fail-closed migration safety.
- Mapping arbitrary names or `SortOrder` could assign incorrect educational meaning.
- Deleting or repointing legacy rows would be destructive.

## Supported-Grade Database Integrity

**Decision**: Add `CHECK (Value IS NULL OR Value IN (7,8,9,10,11,12))` and a unique index on `(CommunityId, Value)` after backfill.

**Rationale**: MySQL unique indexes allow multiple `NULL` values, so unresolved legacy rows coexist while non-null supported values remain unique. The check blocks unsupported new non-null values. Application checks still enforce current-community ownership and supported grade references for new Classes, StudentLicenses, and assignments.

**Alternatives considered**:

- Uniqueness on `CommunityId + Name` would not enforce integer semantics and would conflict with retained legacy text.
- A filtered unique index is not portable to the configured MySQL behavior.
- Application-only uniqueness would provide no concurrent database backstop.

## Migration Boundaries

**Decision**: Use two ordered migrations: `FixedCommunityGrades`, then `TeacherClassAssignments`.

**Rationale**: Grade data transformation requires production preflight and may require manual remediation. Keeping it separate makes audit and rollout explicit. The join-table migration is independent and mechanically reversible.

**Alternatives considered**:

- One combined migration would couple legacy data cleanup to an unrelated join table and make failures harder to diagnose.
- More migrations would not isolate another meaningful deployment boundary.

## New Community Grade Initialization

**Decision**: Add six Grade children to the new Community aggregate before its existing first save.

**Rationale**: EF Core already persists the community graph and supplies the generated Community ID. This creates the Community and supported grades atomically without a new service or repository.

**Alternatives considered**:

- Calling a separate grade seeder after community creation risks temporarily incomplete communities.
- Reusing the obsolete staff CreateGrade command would expose mutable-grade semantics internally.

## Teacher-Class Relationship

**Decision**: Add `TeacherClassAssignment` with `Id`, `TeacherUserId`, `ClassId`, `CreatedAt`, a unique pair index, and restricted User/Class foreign keys. Do not store CommunityId or GradeId.

**Rationale**: It is the smallest proper many-to-many join and matches current model conventions. Community and grade are derived from Class; duplicating them would introduce synchronization risks. Application validation confirms Active Teacher membership and Active same-community Class on a supported Grade.

**Alternatives considered**:

- A `TeacherGrade` table would duplicate information and is explicitly out of scope.
- A serialized class-id list would lose referential integrity and queryability.
- Adding CommunityId to the join is unnecessary because Class already owns tenant context.

## Atomic Assignment Replacement

**Decision**: Implement Teacher class assignment replacement behind a narrow `ITeacherClassAssignmentService` Domain interface with an Infrastructure implementation that owns the relational transaction and locking behavior. The service validates the complete normalized desired set and replaces it atomically for one Teacher. Competing valid replacements are serialized and use normal last-successful-transaction semantics; requests are never merged. The unique Teacher/Class constraint remains the duplicate-pair backstop. Do not add an optimistic concurrency or version column solely for this feature.

**Rationale**: `IBaseRepository<T>` exposes CRUD/query operations but no transaction or row-lock boundary, while existing SprintLabs integrity workflows place a specific Domain interface over an Infrastructure service that owns a serializable `ApplicationDbContext` transaction. Reusing that pattern preserves layer direction and guarantees that each committed replacement is one internally valid complete set, invalid requests make no changes, and two competing valid requests cannot produce a partially combined set. This is a feature-specific integrity service, not a generic UnitOfWork, generic transaction abstraction, or new repository framework.

**Alternatives considered**:

- One `SaveChangesAsync` without a serializing transaction can prevent partial failure within one request but cannot guarantee that two valid replacements are not combined.
- Delete-all then multiple saves could lose assignments after mid-operation failure.
- A generic UnitOfWork, transaction manager, or repository abstraction would broaden architecture without reuse.
- Optimistic concurrency/version columns would add schema and client semantics that are unnecessary when the transaction can serialize one Teacher's replacement.
- Rejecting duplicate request IDs provides no benefit over deterministic normalization required by the spec.

## Teacher Roster Query Shape

**Decision**: Extend the existing ListTeachers query to `PagedRequest`/`PagedResponse`, cap page size at 100, use database predicates/count/paging, batch identity lookup once, and load page assignments in one query.

**Rationale**: The current implementation is unpaged and calls `GetCurrentUser` once per Teacher. `IUserService` already exposes `SearchUserIds` and `GetUsersByIds`, so no interface change is required. Correlated assignment `Any` filters avoid duplicate memberships when a Teacher has multiple same-grade classes.

**Alternatives considered**:

- Loading all Teachers/assignments and paginating in memory is inaccurate and unbounded.
- Joining Identity directly from Application would violate layering and bypass the existing service.
- A custom roster repository is unnecessary for a single EF Core query shape.

## Teacher Roster Visibility

**Decision**: Preserve current Owner-only roster authorization and current membership-status visibility. Apply grade/class matching only through Active classes on supported Grades. Return Active supported assignments; unassigned Teachers remain visible without grade/class filters.

**Rationale**: Assignments are descriptive and must not grant access. Pending/Removed Teacher memberships may still be relevant to an Owner roster, but only an Active Teacher can receive new assignments.

**Alternatives considered**:

- Restricting the entire roster to Active Teachers would silently change existing management behavior.
- Treating assignments as authorization would conflict with CommunityUser membership rules.

## Student Compatibility

**Decision**: Preserve existing Student queries and filters. Enforce supported grades only on new/updated StudentLicenses. Format supported integer values into existing grade-name response fields, with retained legacy Name as a read-only fallback.

**Rationale**: Existing Student list/detail code already filters, counts, pages, searches, batches user/player data, and validates tenant-owned Grade/Class filters correctly. Read filters must remain able to inspect existing records linked to unresolved legacy Grades. Only new writes need the strict supported-grade rule.

**Alternatives considered**:

- Rewriting Student placement would duplicate existing StudentLicense/Class relationships.
- Rejecting legacy Grade IDs on reads would hide records requiring remediation.
- Changing all Student response DTO shapes is unnecessary for this feature.

## Demo Seeder Placement and Gating

**Decision**: Add a static Infrastructure seeder called from Program after migrations, guarded by both Development and `DemoCommunitySeed:Enabled` (default false).

**Rationale**: This follows the current `Infrastructure/Seed` and startup pattern without new DI types. The double guard prevents silent production execution.

**Alternatives considered**:

- Configuration-only gating could be enabled accidentally in production.
- `HasData` cannot use UserManager/password APIs or reconcile relational state.

## Demo Identity and Domain State

**Decision**: Create or reconcile Owner/Teachers with `UserManager`; create deterministic student Identity Users without passwords plus Player, Active Student membership, and Active StudentLicense records using real relationships.

**Rationale**: Existing community login requires confirmed, Active Identity users and Teacher eligibility flags. Student listing/detail derives identity, Player, membership, and license state but does not require student password authentication.

**Alternatives considered**:

- Manually writing password hashes would bypass Identity invariants.
- Fake student DTO rows or incomplete relationships would not exercise real APIs.
- Using Teacher invitations would make demo accounts unavailable until acceptance and would alter the normal workflow.

## Demo Idempotency and Failure Safety

**Decision**: Reconcile stable keys inside one serializable relational transaction and fail on incompatible ownership, membership, role, password, or duplicate-key state.

**Rationale**: Stable keys make repeat and partial-state runs deterministic. A transaction prevents a failed seed from leaving a newly partial relational state. Refusing incompatible data avoids privilege or tenant takeover.

**Alternatives considered**:

- Blind insert-if-missing leaves partially seeded records inconsistent.
- Overwriting a colliding email/slug is unsafe.
- A new generic seed framework is unnecessary for one dataset.

## License Counters

**Decision**: Preserve and reconcile the existing semantics: `UsedStudents` counts non-Revoked StudentLicenses; `UsedTeachers` counts Pending or Active Teacher memberships; Owners do not consume Teacher capacity. Keep maxima at least the actual use and demo baseline without reducing existing capacity.

**Rationale**: Add/revoke and invitation/removal services already implement these meanings. The feature must not invent Teacher-seat or Student-license rules.

**Alternatives considered**:

- Counting only Active students would disagree with Pending license issuance.
- Counting the Owner as a Teacher would change current domain behavior.
- Setting counters to hard-coded demo counts would become incorrect if compatible existing demo-community records exist.

## Focused Verification Workflow

**Decision**: Record focused baseline results before implementation edits, run affected namespace filters after each slice, finish with a solution build and combined focused tests, and reserve the full solution suite for diagnosis or explicit request.

**Rationale**: This matches AGENTS.md and the feature requirement while providing fast feedback for security-sensitive changes.

**Alternatives considered**:

- Running the full suite first is explicitly excluded and wastes feedback time.
- Build-only verification is insufficient for tenant and data-integrity behavior.
