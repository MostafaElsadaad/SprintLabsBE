# Research: Community Student Viewing

## Decision: Use StudentLicenses as the roster source

**Rationale**: Student licenses already represent community-owned student seats and include pending students before login activation. This satisfies the requirement that pending students appear even when `UserId` and `PlayerProfileId` are null.

**Alternatives considered**:

- Query `CommunityUsers` with role Student: rejected because pending licensed students may not have a membership yet.
- Query `Players`: rejected because pending students may not have a player profile and player profiles are not community-scoped by themselves.

## Decision: Authorize through existing community role checks

**Rationale**: `ICommunityAccessService.HasCommunityRole` already checks active membership and role. Passing Owner and Teacher roles matches the feature's access rules and existing SprintLabs authorization style.

**Alternatives considered**:

- Add a new permissions framework: rejected by user constraints and unnecessary for two read-only endpoints.
- Allow platform admin bypass: rejected because current feature rules say platform admin should not bypass membership unless existing conventions already do; current community features generally require role membership.

## Decision: Keep pagination on existing PagedRequest and PagedResponse

**Rationale**: `Shared.Requests.PagedRequest` and `Shared.Responses.PagedResponse<T>` already exist and are supported by `BaseRepository<T>.GetAllAsync(IQueryable<T>, PagedRequest)`.

**Alternatives considered**:

- Create a student-specific pagination response: rejected by explicit constraint and would fragment API consistency.
- Return an unpaged list like older student license listing: rejected because this feature explicitly requires pagination.

## Decision: Add a new read-only Communities/Students CQRS feature

**Rationale**: Student viewing has different authorization and response needs from owner student license management. A separate `Application/Features/Communities/Students/` area keeps list/detail DTOs and query handlers focused without touching license mutation handlers.

**Alternatives considered**:

- Extend `StudentLicenses/ListStudentLicenses`: rejected because that slice is Owner-only license management and currently returns mutation-oriented license DTOs, not a roster/detail view.
- Create feature-specific repositories: rejected because simple filtered reads can use `IBaseRepository<T>.AsQueryable()`.

## Decision: Query related User and Player data for DTO shaping and search only

**Rationale**: The response needs nullable user/player fields and search by license email, user email, user name, and player name. The implementation can use EF Core joins or existing queryable access against existing sets and project into DTOs, while still treating `StudentLicense` as the boundary.

**Alternatives considered**:

- Use only `IUserService.FindByEmail` per row: rejected for list/search because it would create N+1 lookups and make searching by linked user/player fields awkward.
- Add navigation properties to `StudentLicense`: rejected for planning because it may require model changes not needed for a read-only feature.

## Decision: Validate grade/class filters before listing

**Rationale**: Existing grade/class and student-license features validate route-community ownership before using grade/class identifiers. This prevents cross-community leakage and gives deterministic errors for invalid filters.

**Alternatives considered**:

- Silently return empty results for invalid cross-community filters: rejected because existing features tend to reject missing or cross-community grade/class ids.

## Decision: Detail requires non-revoked license linked to player profile in route community

**Rationale**: Detail is keyed by `playerProfileId`, so pending students without player profiles cannot be fetched through this route. Revoked licenses should not grant detail access, and the community boundary must be proven by a matching license row.

**Alternatives considered**:

- Fetch any player profile by id then check community membership: rejected because player profiles are not community-owned.
- Include revoked licenses in detail: rejected by the feature requirement for active or non-revoked licenses.

## Decision: Analytics remains a DTO placeholder

**Rationale**: The feature requires an analytics object but explicitly excludes analytics calculation. A fixed placeholder DTO with zero/null/default values keeps the contract stable for future work.

**Alternatives considered**:

- Compute simple progression metrics: rejected because real analytics calculation is out of scope.
- Omit analytics until later: rejected because detail response requires the placeholder object.

## Decision: No database migration

**Rationale**: Required data already exists on `StudentLicense`, `User`, `Player`, `Grade`, `Class`, and `CommunityUser`.

**Alternatives considered**:

- Add denormalized roster table or analytics table: rejected by explicit no-new-table constraint and YAGNI.
