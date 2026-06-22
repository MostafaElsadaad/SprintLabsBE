# Feature Specification: User Identity Foundation

**Feature Branch**: `001-user-identity-foundation`

**Created**: 2026-06-22

**Status**: Draft

**Input**: User description: "Build the shared user identity layer for Sprint Labs. Create a backend identity foundation that supports both B2C players and future B2B communities/schools. This feature covers Users, PlayerProfiles link to Users, Google login updates, GET /api/users/me, and GET /api/users/me/player-profile. Communities, licenses, teachers, owners, admin community management, grades/classes, and student licenses are out of scope."

## Clarifications

### Session 2026-06-22

- Q: Should the first migration make the player profile user link nullable or required? -> A: Make Player.UserId nullable in the first migration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign in as a Player with Shared Identity (Priority: P1)

A player signs in with Google and receives one stable login identity while preserving the current game profile and progress experience.

**Why this priority**: The game login path is already live behavior. The identity foundation must support the future platform model without breaking player sign-in or progress continuity.

**Independent Test**: Can be fully tested by signing in through the player login flow with a Google account and confirming that the same user identity and player profile are returned across repeated sign-ins.

**Acceptance Scenarios**:

1. **Given** no user exists for a Google email, **When** the player signs in through the player login flow, **Then** a user identity is created and returned with a linked player profile and login token.
2. **Given** a user already exists for a Google email, **When** the same person signs in again, **Then** the existing user identity is returned instead of creating a duplicate.
3. **Given** a player already has game progress, **When** the migration or backfill has linked that player profile to the matching user, **Then** sign-in preserves access to the same player profile and progress.

---

### User Story 2 - Read Current Authenticated Identity (Priority: P2)

An authenticated user can retrieve their own identity details, including whether they currently have a player profile.

**Why this priority**: Client applications need a single identity read to understand who is signed in and which experience is available.

**Independent Test**: Can be fully tested by signing in, calling `GET /api/users/me`, and confirming the response contains only the authenticated user's identity details and profile link status.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a linked player profile, **When** the current identity endpoint is requested, **Then** it returns user id, email, name, avatar, status, platform-admin flag, and player profile id.
2. **Given** an authenticated user without a linked player profile, **When** the current identity endpoint is requested, **Then** it returns the identity details with no player profile id.
3. **Given** no authenticated user is present, **When** the current identity endpoint is requested, **Then** the request is rejected without exposing identity data.

---

### User Story 3 - Read Current Player Profile (Priority: P3)

An authenticated user can retrieve their own player profile when one exists.

**Why this priority**: The game client needs a direct way to load the player's profile after identity is resolved, while non-player identities should remain valid for future platform use.

**Independent Test**: Can be fully tested by signing in as users with and without linked player profiles, then calling `GET /api/users/me/player-profile` and verifying the correct profile or absence response.

**Acceptance Scenarios**:

1. **Given** an authenticated user with a linked player profile, **When** the current player profile endpoint is requested, **Then** it returns that user's player profile.
2. **Given** an authenticated user without a linked player profile, **When** the current player profile endpoint is requested, **Then** it returns a controlled not-found style outcome.
3. **Given** a user is authenticated, **When** the current player profile endpoint is requested, **Then** no other user's player profile can be returned.

### Edge Cases

- A Google account signs in with the same email but a newly available Google account id; the account should be matched to the existing user by email and updated without creating a duplicate.
- A Google login lacks required identity fields such as email or display name; the login should fail with a controlled user-facing error.
- A suspended user attempts to sign in or read current identity data; access should be rejected consistently according to the platform's suspended-user behavior.
- Existing player profiles may not all have a reliable Google identity match during backfill; those records should not be linked incorrectly.
- Two existing records appear to match the same Google email; the system should avoid automatic ambiguous linking and surface the conflict for safe resolution.
- A non-player future platform identity has no player profile; current identity should still work while current player profile reports absence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST represent login identity as a user record with id, optional Google account id, email, name, optional avatar, platform-admin flag, status, created timestamp, and updated timestamp.
- **FR-002**: The system MUST support user statuses of Active and Suspended.
- **FR-003**: The system MUST represent game progress/profile as a player profile that can be linked to one user, with the initial user link allowed to be absent during rollout.
- **FR-004**: A user MUST be linkable to zero or one player profile.
- **FR-005**: A player profile MUST NOT be linkable to more than one user.
- **FR-006**: Existing player profiles MUST have a migration or backfill path that keeps the initial player-to-user link nullable, links profiles to users where a safe match exists, and avoids unsafe ambiguous matches.
- **FR-007**: Google login MUST find an existing user by Google email before creating a new user.
- **FR-008**: A new Google login MUST create a user when no user exists with the same Google email.
- **FR-009**: Repeated Google login with the same email MUST return the same user identity.
- **FR-010**: Google login from the player/game flow MUST create or find the related player profile for that user.
- **FR-011**: Google login MUST return user id, player profile id when available, and an authentication token.
- **FR-012**: The authentication token MUST contain enough user identity information for authenticated requests to resolve the current user reliably.
- **FR-013**: `GET /api/users/me` MUST return the authenticated user's id, email, name, avatar, status, platform-admin flag, and player profile id when one exists.
- **FR-014**: `GET /api/users/me/player-profile` MUST return the authenticated user's player profile when one exists.
- **FR-015**: `GET /api/users/me/player-profile` MUST return a controlled absence outcome when the authenticated user has no player profile.
- **FR-016**: Current-user reads MUST only return data for the authenticated user.
- **FR-017**: Suspended users MUST be prevented from completing authenticated identity actions according to the platform's controlled error behavior.
- **FR-018**: The feature MUST preserve existing game login behavior for current players.
- **FR-019**: The feature MUST NOT introduce communities, community memberships, licenses, teachers, owners, admin community management, grades, classes, or student licenses.
- **FR-020**: All newly introduced responses MUST follow the product's existing success and controlled-error response conventions.

### Key Entities

- **User**: Shared login identity for a person using Sprint Labs. Key information includes id, Google account id when available, email, name, avatar, platform-admin flag, status, and creation/update timestamps.
- **PlayerProfile**: Existing game profile/progress record for a player. It may be linked to one user, but the user link is nullable in the first migration to protect existing profiles during rollout.
- **Authentication Session/Token**: Sign-in result that allows later current-user requests to resolve the authenticated user and, when present, their linked player profile.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successful new Google player sign-ins return a user id, authentication token, and player profile id.
- **SC-002**: 100% of repeated Google sign-ins with the same email return the same user identity.
- **SC-003**: 100% of migrated player profiles with safe identity matches remain accessible through the player login flow with no loss of profile progress.
- **SC-004**: Authenticated users can retrieve their current identity in a single request and see whether a player profile is linked.
- **SC-005**: Authenticated users with linked player profiles can retrieve their own player profile, and users without one receive a clear absence outcome.
- **SC-006**: No tested current-user request exposes another user's identity or player profile data.
- **SC-007**: The feature can be validated without implementing any community, license, teacher, owner, grade, class, or student-license behavior.

## Assumptions

- Existing player/game login must continue to work during and after the identity foundation rollout.
- Email is the primary matching value for Google login because the feature explicitly requires creating or finding a user by Google email.
- Google account id may be attached when available, but absence of a Google account id does not block email-based matching.
- During the first migration, Player.UserId remains nullable so player profiles may temporarily exist without a linked user until a safe match or manual resolution is available.
- The final business rule is one user to zero or one player profile, and one player profile to at most one user.
- Platform-admin users are supported as a flag on user identity, but admin management workflows are outside this feature.
- Communities and B2B membership concepts are deliberately excluded from this slice, even though the identity model should not block them later.
