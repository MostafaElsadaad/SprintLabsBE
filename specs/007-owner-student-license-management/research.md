# Research: Owner Student License Management

## Student License Storage

**Decision**: Add a dedicated `StudentLicense` entity and `StudentLicenses` table.

**Rationale**: The feature tracks license lifecycle, email-change count, grade/class assignment, assigning Owner, optional future user/profile links, and revocation state. This data is not the same as community membership: Pending licenses must reserve seats without creating `CommunityUser` student access.

**Alternatives considered**:

- Use `CommunityUser` only: rejected because Pending licenses must not grant student access and require email-change/license-specific fields.
- Extend `CommunityLicense`: rejected because it stores community-wide capacity, not per-student allocations.

## Domain Model Placement

**Decision**: Add `Domain/Models/StudentLicense.cs` and keep `StudentLicenseStatus` in `Domain/Enums/CommunityEnums.cs`.

**Rationale**: Community-related entities are currently colocated in `Community.cs`, but AGENTS.md now asks for one public class per file unless existing style clearly requires otherwise. `StudentLicense` is large enough to deserve its own file while still following existing namespace and DbContext patterns.

**Alternatives considered**:

- Add `StudentLicense` to `Community.cs`: rejected because that file is already carrying multiple entities and this would worsen the feature’s file structure.
- Create a separate student licensing bounded context: rejected as too broad for this slice.

## Authorization

**Decision**: Use `ICommunityAccessService.HasCommunityRole(userId, communityId, new[] { CommunityUserRole.Owner })` in every handler.

**Rationale**: Previous owner-only features use the community access service in handlers. This keeps authorization membership-based and avoids a new permissions framework.

**Alternatives considered**:

- Controller attributes or policies: rejected because current member-facing community features authorize in handlers.
- Platform-admin bypass: rejected because the spec keeps platform admin access separate from community role access.

## Seat Counting

**Decision**: Treat `CommunityLicense.UsedStudents` as the source of stored used-seat count and update it only when a license begins or stops counting.

**Rationale**: Existing teacher management uses `UsedTeachers` similarly. Pending and Active student licenses count; Revoked licenses do not. Updating a pending email or grade/class assignment does not change usage.

**Alternatives considered**:

- Compute usage dynamically from `StudentLicenses`: rejected because existing capacity model stores used counts and the feature explicitly refers to `UsedStudents`.
- Increment on future activation: rejected because the spec states activation must not increment again.

## Email Normalization and Duplicate Detection

**Decision**: Normalize license emails by trimming and lowercasing before validation, storage, duplicate checks, search, and updates.

**Rationale**: The spec requires duplicates differing only by casing or whitespace to be treated as duplicates. Current owner teacher invite flow normalizes email similarly.

**Alternatives considered**:

- Preserve input casing for uniqueness: rejected because it would allow duplicate seats for the same address.
- Add a separate normalized email column: optional but not required; for MySQL index/query safety the plan can use the stored normalized `Email` value directly.

## Grade/Class Validation

**Decision**: Validate `Grade.CommunityId == route communityId`, `Class.CommunityId == route communityId`, `Class.GradeId == gradeId`, and `Class.Status == Active` for add and update.

**Rationale**: This enforces SaaS tenant isolation and prevents assigning licenses to deleted or cross-community classes.

**Alternatives considered**:

- Validate only class id: rejected because a class-grade mismatch could silently assign the wrong grade context.
- Allow deleted classes for historical updates: rejected because the spec says deleted/inactive classes should not be accepted for new or updated assignment.

## Revoking Active Student Access

**Decision**: If a revoked Active license has a linked `UserId`, set the matching `CommunityUser` row with `Role = Student` for the same community/user to `Removed`.

**Rationale**: This preserves membership history and follows the existing soft-removal pattern for community users. It avoids removing unrelated roles or memberships.

**Alternatives considered**:

- Hard delete `CommunityUser`: rejected because existing community membership removal patterns use status changes.
- Remove every membership for the user: rejected because the spec forbids touching unrelated memberships.

## API Shape

**Decision**: Extend the existing versioned `CommunitiesController` route tree with `student-licenses` endpoints.

**Rationale**: Existing member-facing profile, teacher, grade, and class endpoints all live under `/api/v1/Communities/{communityId}/...` and dispatch thin MediatR commands/queries.

**Alternatives considered**:

- New `StudentLicensesController`: rejected because it would fragment community-scoped member APIs without need.
- Admin endpoints: rejected because this is owner-managed community functionality, not platform admin management.
