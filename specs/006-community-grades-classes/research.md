# Research: Community Grades and Classes

## Decision 1: Reuse Existing Community Role Checks

**Decision**: All grade and class handlers will call `HasCommunityRole(userId, communityId, new[] { CommunityUserRole.Owner, CommunityUserRole.Teacher })`.

**Rationale**: The access service already defines Active membership and role matching. Reusing it keeps Pending and Removed memberships from granting access and preserves platform-admin access as separate from community role access.

**Alternatives considered**:

- Query `CommunityUser` directly in every handler: rejected because it duplicates existing role semantics.
- Add policies, filters, or a new permissions framework: rejected because the project has no need for a new authorization layer here and the feature forbids it.
- Let platform admins bypass membership checks: rejected because previous community features keep platform-admin access separate.

## Decision 2: Add Grade and Class as Community-Owned Entities

**Decision**: Add `Grade` and `Class` entities with required `CommunityId`, and require `Class.GradeId` to reference a grade in the same community.

**Rationale**: Grades and classes are tenant/community-owned SaaS data. Storing `CommunityId` on both entities keeps queries directly scoped to the route community and makes ownership checks straightforward.

**Alternatives considered**:

- Derive class community only through grade: rejected because direct `CommunityId` on Class simplifies community-scoped filtering and tenant isolation checks.
- Add school/tenant abstractions: rejected as unrelated architecture expansion.

## Decision 3: Use ClassStatus for Soft Delete

**Decision**: Add a `ClassStatus` enum with `Active = 1` and `Deleted = 2`. Class list and grade class counts filter to Active classes.

**Rationale**: The existing project uses status enums for lifecycle state (`CommunityStatus`, `CommunityUserStatus`, `UserStatus`) and has no general `IsDeleted` convention. A status enum fits local style and makes response state explicit.

**Alternatives considered**:

- Add `IsDeleted`: viable, but rejected because status enums are the local convention.
- Hard delete classes: rejected by the feature.
- Reuse `CommunityUserStatus`: rejected because membership state names do not belong to classes.

## Decision 4: Keep Grade Lifecycle Minimal

**Decision**: Implement only grade creation and grade listing. Do not add grade update, delete, status, or soft delete.

**Rationale**: The feature explicitly excludes grade update/delete endpoints. Adding grade lifecycle state would create future scope and extra migration surface that current workflows do not need.

**Alternatives considered**:

- Add `GradeStatus`: rejected because no grade deletion behavior is in scope.
- Add grade update endpoint: rejected as explicitly out of scope.

## Decision 5: Use Existing Generic Repositories

**Decision**: Handlers will inject `IBaseRepository<Grade>` and `IBaseRepository<Class>` plus existing repositories as needed, using `AsQueryable()` for ownership checks, filters, counts, and projections.

**Rationale**: Required operations are simple create, list, update, and status-change workflows that the generic repository supports. No query is complex or reused enough to justify a custom repository.

**Alternatives considered**:

- Add grade/class repositories: rejected because they would only wrap one-line EF Core queries.
- Use `GetByIdAsync`: rejected for long IDs because the base method accepts `int`.

## Decision 6: Configure EF Core in ApplicationDbContext

**Decision**: Add `DbSet<Grade>`, `DbSet<Class>`, relationship configuration, max lengths, required fields, defaults, and indexes in `ApplicationDbContext.OnModelCreating`.

**Rationale**: Existing community entities are configured inline in `ApplicationDbContext`. Following that style avoids introducing a new configuration pattern.

**Alternatives considered**:

- Separate `IEntityTypeConfiguration` classes: rejected because the project currently configures these models inline.
- No explicit configuration: rejected because max lengths, defaults, indexes, and relationships are important data-integrity controls.

## Decision 7: Index for Community-Scoped Queries

**Decision**: Add indexes for `Grade.CommunityId`, `Grade.CommunityId + SortOrder`, `Class.CommunityId`, `Class.GradeId`, and `Class.CommunityId + Status`.

**Rationale**: Every endpoint filters by community, grade, and active/deleted status. These indexes match the access patterns without adding uniqueness rules that the specification does not require.

**Alternatives considered**:

- Unique grade names or class names: rejected because uniqueness is not required by the spec and may block legitimate school naming conventions.
- Only foreign-key indexes: viable but less aligned with class list and count filters.

## Decision 8: Focused Handler and EF Tests

**Decision**: Add focused tests for Owner/Teacher access, Student/inactive denial, grade ownership, class ownership, class soft delete exclusion, and class count behavior.

**Rationale**: Authorization and community ownership are the highest-risk areas. Handler tests can validate behavior without heavy server integration.

**Alternatives considered**:

- Build-only validation: rejected because tenant isolation and soft-delete counts are non-trivial.
- Controller-only tests: rejected because business logic belongs in handlers.
