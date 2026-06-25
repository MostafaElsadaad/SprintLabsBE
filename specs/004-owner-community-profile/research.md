# Research: Owner Community Profile

## Decision 1: Reuse the Existing Community Access Service

**Decision**: The GET handler will call `CanAccessCommunity(userId, communityId)`. The PATCH handler will call `HasCommunityRole(userId, communityId, new[] { CommunityUserRole.Owner })`.

**Rationale**: These methods already define Active membership and role semantics. Reusing them prevents this feature from creating a second authorization interpretation and keeps platform-admin access separate.

**Alternatives considered**:

- Query `CommunityUser` directly in each handler: rejected because it duplicates the previous feature's reusable access rules.
- Add policies, filters, or a new permissions framework: rejected because the project has no established role-filter pattern and the feature explicitly forbids a new permissions framework.
- Allow `IsPlatformAdmin` to bypass membership checks: rejected because platform-admin access is deliberately separate.

## Decision 2: Preserve Existing Suspended-User Behavior

**Decision**: Each handler will resolve the current user through `IUserService.GetCurrentUser` and reject a missing or suspended user before evaluating community access.

**Rationale**: Existing authenticated current-user/community handlers explicitly enforce this behavior, while `ICommunityAccessService` intentionally checks membership only. This preserves current account-status behavior without changing the access service contract.

**Alternatives considered**:

- Change `ICommunityAccessService` to include user status: rejected because it broadens an existing service and would mix platform identity status into membership semantics.
- Depend only on JWT validity: rejected because a still-valid token can belong to a user later marked Suspended.

## Decision 3: Add a Dedicated Communities Controller

**Decision**: Add `CommunitiesController` with versioned route `api/v{version:apiVersion}/[controller]`, class-level `[Authorize]`, and GET/PATCH actions at `{communityId:long}`.

**Rationale**: These are member-facing community endpoints, not platform-admin operations. A dedicated controller preserves the existing route convention and avoids modifying `AdminCommunitiesController`.

**Alternatives considered**:

- Add actions to `AdminCommunitiesController`: rejected because authorization and audience differ.
- Add actions to `UsersController`: rejected because the resource is a selected community, not the current-user identity aggregate.

## Decision 4: Use the Generic Community Repository

**Decision**: Both handlers will inject `IBaseRepository<Community>` and use `AsQueryable()` with EF Core async operations. PATCH will use `UpdateAsync` and `SaveChangesAsync`.

**Rationale**: The queries are simple, local, and fully supported by the established generic repository. Community IDs are `long`, while `GetByIdAsync` currently accepts `int`.

**Alternatives considered**:

- Reintroduce a custom community repository: rejected because it would only wrap simple one-line queries.
- Use `GetByIdAsync`: rejected because its `int` signature is unsafe for `long` community IDs.

## Decision 5: Authorization Before Resource Retrieval

**Decision**: After validating the current user and positive community id, handlers will perform membership/role authorization before returning community data or applying updates.

**Rationale**: Unauthorized users should not learn community profile details. Membership checks already use exact community IDs and return false for absent, Pending, or Removed membership.

**Alternatives considered**:

- Retrieve the community first and return 404 to every caller: rejected because it reveals existence to users without access.
- Return 404 for failed membership: rejected because existing project conventions use controlled forbidden outcomes for authorization failure.

## Decision 6: Normalize and Validate Profile Updates Consistently

**Decision**: PATCH requires both name and slug. Values are trimmed; slug is lowercased. Duplicate detection checks `Slug == normalizedSlug && Id != communityId`, allowing the same community to retain its slug.

**Rationale**: This matches admin community creation and satisfies both uniqueness and same-slug acceptance requirements.

**Alternatives considered**:

- Treat PATCH as a partial update: rejected because the feature request supplies both fields and the specification assumes both are required.
- Preserve slug casing: rejected because existing creation behavior stores lowercase slugs.
- Rely only on the database unique index: rejected because a controlled validation response is clearer and avoids exception-driven normal flow.

## Decision 7: No Database Change

**Decision**: Reuse `Community.Name`, `Slug`, `Status`, and nullable `UpdatedAt`; do not generate a migration.

**Rationale**: All required fields, uniqueness constraints, and timestamp storage already exist.

**Alternatives considered**:

- Add profile or audit tables: rejected as out of scope.
- Add a new updated-by field: rejected because the feature only requires the established update timestamp behavior.

## Decision 8: Focused Handler Tests

**Decision**: Test GET access for Active Owner/Teacher/Student and denial for absent/Pending/Removed membership. Test PATCH Owner success, Teacher/Student/inactive denial, duplicate slug rejection, same-slug acceptance, normalization, timestamp update, and no mutation on failure.

**Rationale**: Authorization and slug uniqueness are the highest-risk logic. Handler tests can validate them without introducing a new integration-test framework.

**Alternatives considered**:

- Controller-only tests: rejected because the critical behavior lives in handlers and services.
- No tests beyond build: rejected because role isolation and uniqueness are non-trivial.
