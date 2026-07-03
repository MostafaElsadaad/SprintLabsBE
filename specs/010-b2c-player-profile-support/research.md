# Research: B2C Player Profile Support

## Decision: Preserve Google login as the B2C entry point

**Rationale**: Existing Google login already creates or finds a `User`, creates or finds a `Player`, and does not require community membership before returning the login response. Pending teacher and student activation are side effects for users that have matching records, not prerequisites for B2C login.

**Alternatives considered**:

- Add a separate B2C login endpoint: rejected because it would duplicate authentication behavior and risk drift.
- Disable student activation for B2C users: rejected because activation is already conditional on pending licenses.

## Decision: Reuse current player profile read endpoint

**Rationale**: `GET /api/v1/Users/me/player-profile` already resolves the current user from the JWT claim, checks user status, and fetches the player profile by user id. It does not require `CommunityUser` or `StudentLicense`.

**Alternatives considered**:

- Add a duplicate `GET /api/v1/PlayerProfiles/me`: rejected as unnecessary for the current feature unless a later frontend contract explicitly requires it.

## Decision: Add a self-scoped PATCH endpoint if missing

**Rationale**: The requested contract is `PATCH /api/player-profiles/me`. The current project has `PUT /api/v1/Account/profile`, but it is GoogleId-scoped and allows name updates. A dedicated current-player-profile patch should be scoped by authenticated user id and limited to `Age`, `Grade`, and `SchoolName`.

**Alternatives considered**:

- Reuse `PUT /api/v1/Account/profile`: rejected because it is not the requested route or method, and it is broader than the B2C profile edit scope.
- Accept a player profile id in the route/body: rejected because the feature explicitly requires not updating another user's profile.

## Decision: No migration for profile fields

**Rationale**: The current `Player` model already has nullable `Age`, nullable `Grade`, and nullable `SchoolName`.

**Alternatives considered**:

- Add a migration proactively: rejected because no missing field was found and the feature forbids migrations unless required.

## Decision: Keep profile update validation small

**Rationale**: Existing profile update behavior validates grade range. This feature should validate clearly invalid age and grade values without inventing a broader profile policy.

**Alternatives considered**:

- Add a full validation framework: rejected by existing project conventions and YAGNI.
- Leave all numeric values unchecked: rejected because the spec requires invalid age/grade rejection.

## Decision: Analytics context is documentation-only

**Rationale**: The feature requires future analytics to distinguish B2C and B2B by nullable community context, but explicitly excludes real analytics calculation. Planning should document that B2C uses `CommunityId = null` and B2B uses `CommunityId = communityId`.

**Alternatives considered**:

- Add analytics events or tables now: rejected as out of scope.
- Add `CommunityId` to PlayerProfile: rejected because B2C profiles should not require community membership and B2B student context already comes from community-bound records.
