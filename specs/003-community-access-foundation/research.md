# Research: Community Access Foundation

## Decision: Add a small community access service before filters

**Rationale**: The project currently uses `[Authorize]` and handler-level checks, but does not have a custom authorization filter pattern for resource-scoped roles. A small service with `CanAccessCommunity(userId, communityId)` and `HasCommunityRole(userId, communityId, roles)` gives future handlers a reusable, testable access primitive without inventing a new authorization framework.

**Alternatives considered**:
- Custom attributes/filters now: rejected because no existing project pattern exists and resource id extraction can vary per endpoint.
- Inline membership queries in every handler: rejected because it duplicates security logic and makes future endpoints easier to get wrong.

## Decision: Keep platform-admin authorization separate

**Rationale**: Admin Community Foundation uses `Users.IsPlatformAdmin` to protect `/api/admin/*`. This feature is about community membership access and must not let platform admin status silently bypass community role checks.

**Alternatives considered**:
- Treat platform admins as implicit owners in all communities: rejected because the specification explicitly says platform admin access is separate and should not replace community role access.

## Decision: Return only active memberships from current-user community list

**Rationale**: The spec prefers excluding pending memberships for access safety and requires removed memberships to be excluded. Returning only Active memberships keeps the list aligned with access checks.

**Alternatives considered**:
- Return Pending memberships for invitation UX: rejected because invite/onboarding workflows are out of scope.
- Return Removed memberships for audit history: rejected because history/admin reporting is out of scope.

## Decision: Extend the existing Community repository for membership reads

**Rationale**: CommunityRepository already owns aggregate community queries. Adding active-membership list and membership role lookup methods keeps EF queries in Infrastructure while keeping handlers and services small.

**Alternatives considered**:
- New repository just for CommunityUser: rejected as extra surface for the same aggregate in this small feature.
- Query DbContext directly from Application handlers: rejected because existing architecture keeps EF behind Infrastructure repositories.

## Decision: No schema or migration planned

**Rationale**: Admin Community Foundation already created Communities, CommunityUsers, CommunityLicenses, and role/status enums. This feature reads existing data and adds behavior only.

**Alternatives considered**:
- Add indexes now for user membership queries: deferred unless implementation or profiling shows the existing unique/index set is insufficient. The feature should first use the existing schema.
