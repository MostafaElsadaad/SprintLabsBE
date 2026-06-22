# Research: Admin Community Foundation

## Decision: Enforce admin access with existing JWT authentication plus user lookup

**Rationale**: Existing controllers use `[Authorize]`, extract the `userId` claim, and let application handlers validate user state. This feature should protect all admin routes with `[Authorize]`, pass the authenticated user id into admin commands/queries, and use `IUserService` to ensure the user exists, is active, and has `IsPlatformAdmin = true`.

**Alternatives considered**:
- ASP.NET authorization policy/filter: rejected for this slice because there is no existing project-specific policy pattern, and the user explicitly asked not to rewrite authentication.
- Controller-only admin checks: rejected because business authorization should be testable at the command/query handler boundary and not rely only on routing.

## Decision: Keep community entities in Domain and configure User relationship by UserId

**Rationale**: `User` currently lives in Infrastructure because it extends the existing Identity user type. Domain community entities should not reference the Infrastructure `User` class. `CommunityUser` will store `UserId`, and EF can configure the relationship to `Infrastructure.DataAccess.User` in `ApplicationDbContext` without a domain navigation property.

**Alternatives considered**:
- Move User into Domain: rejected because it would be a broad identity refactor outside the feature.
- Add an Infrastructure User navigation into Domain model: rejected because it would invert the existing project dependency direction.

## Decision: Use a small Community repository for aggregate reads and idempotent writes

**Rationale**: Listing communities with owner and license summaries, finding by slug, assigning owners idempotently, and upserting licenses are aggregate operations that are cleaner in a feature repository than scattered generic repository queries. The repository can still follow the existing `PlayerRepository` style: a small Domain interface and an Infrastructure implementation over `ApplicationDbContext`.

**Alternatives considered**:
- Use only `IBaseRepository<T>`: rejected because it would push include/filter/upsert details into handlers and increase duplication.
- Create a generic admin service layer: rejected as unnecessary abstraction for the current feature.

## Decision: Normalize email and slug before comparison

**Rationale**: The spec requires duplicate prevention for slugs and owner user reuse by email. Normalizing by trimming and comparing case-insensitively matches existing email lookup behavior in `UserService` and avoids obvious duplicate records.

**Alternatives considered**:
- Store only raw user input and rely on database collation: rejected because behavior would depend on database collation and be harder to test.
- Add separate normalized slug column now: rejected unless implementation discovers MySQL collation/index behavior cannot safely enforce the desired uniqueness with the existing style.

## Decision: License upsert preserves usage counts

**Rationale**: The feature configures limits only. Usage counters belong to future teacher/student consumption workflows, so license updates should change max limits and student email change limit while preserving `UsedStudents` and `UsedTeachers`. New license records initialize both used counts to zero.

**Alternatives considered**:
- Allow admin license requests to overwrite used counts: rejected because it could destroy future usage data and conflicts with the feature constraints.

## Decision: Use separate enum types for community status, membership role, and membership status

**Rationale**: The values have separate meanings and validation rules: community lifecycle, membership role, and membership lifecycle. Separate enums keep EF configuration and handler checks explicit.

**Alternatives considered**:
- Use strings: rejected because existing domain style uses enums for status-like values and EF can persist enum values consistently.
