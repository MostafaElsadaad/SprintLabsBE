# Feature Specification: B2C Player Profile Support

**Feature Branch**: `010-b2c-player-profile-support`

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "Feature name: B2C Player Profile Support. Build and verify B2C player profile support for Sprint Labs. B2C players can play the game without belonging to a community. They should have a User and PlayerProfile only, and should be able to view/update their game profile. This feature covers confirming game login does not require CommunityUser or StudentLicense, GET /api/users/me/player-profile, PATCH /api/player-profiles/me, editable PlayerProfile fields age, grade, schoolName, and ensuring future analytics can distinguish B2C and B2B with nullable CommunityId. It excludes community membership creation, student license creation, school dashboard analytics, real analytics calculation, reports, assignments, parent accounts, and payment logic."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - B2C Player Can Log In Without Community Data (Priority: P1)

A player using Sprint Labs directly signs in with Google and receives a User and PlayerProfile even when the player has no community membership and no student license.

**Why this priority**: B2C access is impossible if login assumes every player belongs to a school community or has a student license. This is the foundation for non-school gameplay.

**Independent Test**: Can be fully tested by signing in with a Google account that has no community membership and no student license, then confirming login succeeds and returns the player's identity and player profile information.

**Acceptance Scenarios**:

1. **Given** a Google account with no CommunityUser record, **When** the player signs in, **Then** login succeeds and a User and PlayerProfile exist.
2. **Given** a Google account with no StudentLicense record, **When** the player signs in, **Then** login succeeds and does not require student activation.
3. **Given** an existing B2C player with a User and PlayerProfile only, **When** the player signs in again, **Then** the same account path remains usable without community membership.
4. **Given** existing B2B student activation or teacher/owner login behavior, **When** those users sign in, **Then** their existing login behavior still works.

---

### User Story 2 - B2C Player Can View Their Player Profile (Priority: P2)

A signed-in B2C player opens their profile and sees their current game profile without needing a community or student license.

**Why this priority**: Players need to see their own game identity and progression before editing or playing from their profile.

**Independent Test**: Can be fully tested by signing in as a B2C player with no community membership, calling the current-player-profile endpoint, and confirming the response returns only the signed-in player's profile.

**Acceptance Scenarios**:

1. **Given** an authenticated B2C player with a PlayerProfile, **When** the player views their profile, **Then** the profile is returned.
2. **Given** the player has no CommunityUser membership, **When** the player views their profile, **Then** the request still succeeds.
3. **Given** the player has no StudentLicense, **When** the player views their profile, **Then** the request still succeeds.
4. **Given** one player is authenticated, **When** the profile is requested, **Then** another user's profile is not returned.

---

### User Story 3 - B2C Player Can Update Optional Profile Fields (Priority: P3)

A signed-in B2C player updates optional profile fields so their age, grade, and school name can reflect their personal game profile.

**Why this priority**: Profile editing is the main new B2C profile action. It must be self-scoped and not tied to school membership.

**Independent Test**: Can be fully tested by signing in as a B2C player with no community membership, updating age, grade, and school name, and confirming the returned and later fetched profile contains the updated values.

**Acceptance Scenarios**:

1. **Given** an authenticated B2C player, **When** the player updates age, grade, and school name, **Then** the player's own PlayerProfile is updated and returned.
2. **Given** optional profile fields are omitted or set to null, **When** the player updates the profile, **Then** nullable fields are accepted according to existing profile rules.
3. **Given** a player attempts to update a profile, **When** the request is processed, **Then** only the current user's PlayerProfile can be updated.
4. **Given** the player has no CommunityUser or StudentLicense, **When** the player updates their profile, **Then** the update still succeeds.

---

### User Story 4 - Preserve Analytics Context Boundaries (Priority: P4)

The system remains ready to distinguish future B2C game analytics from B2B school analytics without requiring community membership for B2C players.

**Why this priority**: Future analytics must not accidentally treat B2C gameplay as school-scoped data or require community ownership where none exists.

**Independent Test**: Can be fully tested by reviewing and validating affected profile/login behavior so no B2C path requires CommunityId, while documenting that future B2C analytics use null CommunityId and B2B analytics use the community id.

**Acceptance Scenarios**:

1. **Given** B2C gameplay or profile activity is discussed by this feature, **When** analytics context is represented for future work, **Then** B2C context is understood as `CommunityId = null`.
2. **Given** B2B school gameplay or analytics is handled by future work, **When** community context exists, **Then** B2B context is understood as `CommunityId = communityId`.
3. **Given** this feature is implemented, **When** it is reviewed, **Then** it does not implement real analytics calculations.
4. **Given** B2C profile actions are used, **When** they complete, **Then** they do not create community membership or student licenses.

### Edge Cases

- A new Google login has no existing User, PlayerProfile, CommunityUser, or StudentLicense.
- An existing User has no PlayerProfile before login and no community records.
- An existing B2C PlayerProfile has null age, grade, and school name.
- A profile update sets one or more editable fields to null.
- A profile update uses invalid age or grade values.
- A user has a PlayerProfile but also later receives a StudentLicense; existing student activation behavior must remain compatible.
- A Teacher or Owner signs in with no StudentLicense; their login must still work.
- A suspended or invalid-auth user attempts to access profile endpoints.
- A request attempts to update another user's profile by sending or guessing profile identifiers.
- Future analytics references must not imply that B2C gameplay requires a community id.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a B2C player to sign in with Google without having any CommunityUser membership.
- **FR-002**: The system MUST allow a B2C player to sign in with Google without having any StudentLicense.
- **FR-003**: Google login MUST create or find a User for a B2C player.
- **FR-004**: Google login MUST create or find a PlayerProfile for a B2C player.
- **FR-005**: B2C login MUST NOT create a CommunityUser membership.
- **FR-006**: B2C login MUST NOT create a StudentLicense.
- **FR-007**: Existing B2B student activation behavior MUST continue to work.
- **FR-008**: Existing Teacher and Owner login behavior MUST continue to work.
- **FR-009**: An authenticated user MUST be able to view their own PlayerProfile through the current-player-profile endpoint.
- **FR-010**: Viewing the current PlayerProfile MUST NOT require CommunityUser membership.
- **FR-011**: Viewing the current PlayerProfile MUST NOT require a StudentLicense.
- **FR-012**: An authenticated user MUST be able to update only their own PlayerProfile.
- **FR-013**: Updating the current PlayerProfile MUST NOT require CommunityUser membership.
- **FR-014**: Updating the current PlayerProfile MUST NOT require a StudentLicense.
- **FR-015**: PlayerProfile updates MUST support nullable age, nullable grade, and nullable school name.
- **FR-016**: PlayerProfile updates MUST validate age and grade so clearly invalid profile data is rejected.
- **FR-017**: Profile responses MUST return updated PlayerProfile data without exposing another user's profile.
- **FR-018**: The feature MUST preserve a future analytics convention where B2C context uses null CommunityId and B2B school context uses the relevant community id.
- **FR-019**: The feature MUST NOT implement real analytics calculations.
- **FR-020**: The feature MUST NOT implement community membership creation, student license creation, school dashboard analytics, reports, assignments, parent accounts, or payment logic.
- **FR-021**: The feature MUST NOT add a new database table.
- **FR-022**: The feature MUST NOT add a migration unless existing PlayerProfile storage lacks age, grade, or school name.

### Key Entities *(include if feature involves data)*

- **User**: The authenticated account identity created or found by Google login. A B2C User can exist without community memberships or student licenses.
- **PlayerProfile**: The game profile linked to a User. It stores gameplay profile fields, including nullable age, nullable grade, nullable school name, and existing progression values.
- **Community Membership**: A school/community access record. This feature confirms B2C players do not require one for login or personal profile access.
- **Student License**: A school-issued student seat. This feature confirms B2C players do not require one for login or personal profile access.
- **Analytics Context**: A future reporting boundary where B2C activity is represented without a community id and B2B activity is represented with the relevant community id.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested B2C Google logins without CommunityUser records succeed.
- **SC-002**: 100% of tested B2C Google logins without StudentLicense records succeed.
- **SC-003**: 100% of tested B2C users can view their own PlayerProfile after login.
- **SC-004**: 100% of tested B2C users can update age, grade, and school name on their own PlayerProfile.
- **SC-005**: 100% of tested attempts to update another user's PlayerProfile are prevented.
- **SC-006**: 100% of tested profile view/update flows succeed without creating community memberships or student licenses.
- **SC-007**: Existing tested B2B student activation, Teacher login, and Owner login flows continue to pass.
- **SC-008**: The feature can be demonstrated without adding database tables, reports, assignments, parent accounts, payment logic, or real analytics calculations.
- **SC-009**: Future analytics documentation clearly distinguishes B2C null-community context from B2B community-scoped context.

## Assumptions

- Existing Google login remains the entry point for B2C players.
- Existing User and PlayerProfile identity behavior is reused.
- A B2C player is defined by having a User and PlayerProfile without requiring active community membership.
- Existing PlayerProfile storage already includes age, grade, and school name unless planning discovers otherwise.
- Profile editing applies only to the authenticated user's own PlayerProfile.
- Community membership and student license flows remain separate from B2C profile support.
- Analytics context is documented and preserved as a design rule only; no analytics events or calculations are added by this feature.
