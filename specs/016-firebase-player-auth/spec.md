# Feature Specification: Firebase Player Authentication

**Feature Branch**: `016-firebase-player-auth`

**Created**: 2026-07-29

**Status**: Draft

**Input**: User description: "Add Firebase registration and login for Unity players through one backend login endpoint, safely link legacy Google identities, preserve player progression and existing business activation behavior, keep Google login operational, and enforce secure Firebase token verification and identity-conflict handling."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register or Sign In a Firebase Player (Priority: P1)

A Unity player signs in with Firebase and sends the resulting identity token to SprintLabs. On the first valid sign-in, SprintLabs creates one user and one player profile; on every later sign-in, it returns those same records and a SprintLabs access token.

**Why this priority**: This is the primary player entry point and must support both first-time registration and repeat login without a separate registration API.

**Independent Test**: Submit a valid Firebase identity token for a new player, repeat the same login, and verify both responses identify the same user and player while only one of each record exists.

**Acceptance Scenarios**:

1. **Given** a valid Firebase identity with no SprintLabs account, **When** the player submits the identity token, **Then** one active SprintLabs user and one linked player profile are created and a successful login response is returned.
2. **Given** the Firebase identity already belongs to a SprintLabs user and player, **When** the player signs in again, **Then** the same user and player are returned without duplicate records or reset progression.
3. **Given** the new player has no community membership or student license, **When** the player signs in, **Then** login succeeds as a B2C player.
4. **Given** a new player profile is created, **When** login completes, **Then** all existing progression defaults are retained, including Gold 0, Experience 0, and Level 1.
5. **Given** a successful Firebase login, **When** the response is returned, **Then** it uses the existing login response envelope and includes AccessToken, UserId, PlayerProfileId, Name, Email, PictureUrl, Gold, Experience, and Level.

---

### User Story 2 - Safely Link an Existing Google Player (Priority: P1)

An existing SprintLabs player who previously used Google can start using Firebase without losing their account, profile, progression, memberships, or licenses.

**Why this priority**: Existing players must not receive duplicate accounts or lose progress during the authentication transition.

**Independent Test**: Start with a legacy Google user and player, sign in with a Firebase token containing the same verified Google provider identity, and verify the existing records are linked to the Firebase UID and returned unchanged.

**Acceptance Scenarios**:

1. **Given** no user has the Firebase UID and a legacy user has the verified Google provider identity from the Firebase token, **When** Firebase login occurs, **Then** the Firebase UID is linked to that user and the existing player is reused.
2. **Given** neither Firebase UID nor Google provider identity resolves a user and the token contains a verified email matching one user, **When** Firebase login occurs, **Then** that user is linked to the Firebase UID and its existing player is reused or safely attached.
3. **Given** the token's email is not verified, **When** no Firebase UID or verified provider identity resolves an account, **Then** the email is not used to link an existing account.
4. **Given** one external identity points to one user while another verified identity points to a different user, **When** Firebase login occurs, **Then** the request fails with an identity conflict and no account, player, or identity link is changed.
5. **Given** a legacy player is attached during linking, **When** the update is stored, **Then** the player is linked by internal UserId and no Firebase UID is written into the player's legacy Google identity field.

---

### User Story 3 - Preserve Login-Time Business Rules (Priority: P2)

An eligible player receives the same post-login account protections and onboarding outcomes when entering through Firebase.

**Why this priority**: Adding a new authentication source must not bypass suspension, invitations, licenses, or community access rules.

**Independent Test**: Sign in representative suspended, B2C, pending-teacher, and pending-student accounts through Firebase and verify each receives the same required business outcome without changing seat or invitation rules.

**Acceptance Scenarios**:

1. **Given** the resolved SprintLabs user is suspended, **When** Firebase login is attempted, **Then** login is denied and no access token is issued.
2. **Given** the resolved user has eligible pending teacher memberships, **When** Firebase login succeeds, **Then** those memberships activate under the existing invitation and membership rules.
3. **Given** the resolved user has eligible pending student licenses, **When** Firebase login succeeds, **Then** those licenses activate and link to the resolved user and player under the existing license rules.
4. **Given** the player has neither a community membership nor a student license, **When** Firebase login succeeds, **Then** no membership or license is created merely to permit B2C login.
5. **Given** login-time activation is repeated, **When** the player signs in again, **Then** no duplicate memberships, profiles, licenses, or seat usage changes are introduced.

---

### User Story 4 - Reject Untrusted or Conflicting Identities (Priority: P2)

A player receives a safe authentication failure when the submitted identity cannot be trusted, and a safe conflict response when trusted external identities disagree.

**Why this priority**: Authentication must not allow account takeover, cross-project tokens, accidental merges, or leakage of provider details.

**Independent Test**: Submit malformed, expired, wrong-project, incomplete, unverified-email-linking, and conflicting-identity cases and verify the documented status, absence of data changes, and safe error response.

**Acceptance Scenarios**:

1. **Given** a missing, malformed, expired, revoked, incorrectly signed, or wrong-project Firebase token, **When** login is attempted, **Then** the request returns HTTP 401 and no SprintLabs token is issued.
2. **Given** a verified token lacks the Firebase UID or email required for the SprintLabs player identity, **When** login is attempted, **Then** the request returns HTTP 401 without creating records.
3. **Given** verified identities resolve to different existing users, **When** login is attempted, **Then** the request returns HTTP 409 without merging or modifying either account.
4. **Given** Firebase reports an internal verification detail, **When** the request fails, **Then** the public response does not expose the raw provider error or submitted token.

---

### User Story 5 - Preserve Google Login and User-Owned Profile Updates (Priority: P3)

Existing Google players continue to sign in as before, and players authenticated through either method update only the profile owned by the internal user in their SprintLabs access token.

**Why this priority**: The new login path must be backward compatible and must remove dependence on provider-specific identifiers from authenticated profile ownership.

**Independent Test**: Run the existing Google login regression suite, then use access tokens issued from both Google and Firebase login to update the current player's profile and verify resolution uses the internal user claim.

**Acceptance Scenarios**:

1. **Given** an existing Google player, **When** the existing Google login endpoint is used, **Then** its route, accepted input, response shape, account reuse, player reuse, token generation, and current post-login behavior remain operational.
2. **Given** a Firebase-authenticated player with a valid SprintLabs access token, **When** the player updates their profile, **Then** the profile associated with the internal userId claim is updated.
3. **Given** a Google-authenticated player with a valid SprintLabs access token, **When** the player updates their profile, **Then** the same internal userId ownership rule applies.
4. **Given** an access token lacks a valid internal userId claim, **When** a current-player profile update is attempted, **Then** the request is unauthorized and no profile is changed.
5. **Given** external subject, Google ID, or Firebase UID values differ from internal identifiers, **When** a profile update occurs, **Then** those external values are not used to choose the player profile.

### Edge Cases

- Two first-login requests for the same Firebase UID arrive concurrently.
- Two different Firebase UIDs present the same verified email concurrently.
- A Firebase UID already belongs to one user while its verified Google provider identity or verified email belongs to another.
- A verified Google provider identity matches a user, but that user already has a different Firebase UID.
- A verified email matches a user whose existing Google identity conflicts with the Google provider identity in the Firebase token.
- An unverified email matches an existing user or legacy player.
- A first-time Firebase identity has a required email but no display name or picture.
- A legacy user exists without a player, a legacy player exists without UserId, or the user and player are linked inconsistently.
- A legacy player candidate is already attached to another internal user.
- A player has both eligible pending teacher memberships and pending student licenses.
- A same-community membership has a role that conflicts with a pending student activation.
- A suspended user presents an otherwise valid Firebase token.
- A login is repeated after progression values have changed.
- Firebase token verification is unavailable or application credentials are misconfigured.
- Multiple service instances receive login requests while Firebase initialization is occurring.
- A caller submits a Firebase token to the Google endpoint or a Google token directly to the Firebase endpoint.

## Requirements *(mandatory)*

### Functional Requirements

#### Firebase Login Contract

- **FR-001**: The system MUST provide `POST /api/v1/Account/firebase-login` for Firebase player registration and login.
- **FR-002**: The endpoint MUST accept a request body containing one required `idToken` string.
- **FR-003**: Registration and login MUST use the same endpoint; clients MUST NOT need a separate SprintLabs registration call.
- **FR-004**: A successful request MUST return the existing `BaseResponse<LoginResponse>` structure.
- **FR-005**: The successful response data MUST contain AccessToken, UserId, PlayerProfileId, Name, Email, PictureUrl, Gold, Experience, and Level.
- **FR-006**: The access token returned after Firebase verification MUST be the existing SprintLabs access token used by current player APIs.
- **FR-007**: A Firebase login MUST complete without requiring community membership or a student license.

#### Token Trust and Configuration

- **FR-008**: The system MUST verify every submitted Firebase identity token against the configured Firebase project before resolving or creating SprintLabs data.
- **FR-009**: Verification MUST validate token integrity, lifetime, issuer, audience/project, and revocation or other invalid state supported by the trusted Firebase verifier.
- **FR-010**: Missing, malformed, expired, revoked, incorrectly signed, or wrong-project tokens MUST return HTTP 401.
- **FR-011**: A verified token MUST contain a non-empty Firebase UID and non-empty email required for a SprintLabs player identity; missing required identity data MUST return HTTP 401.
- **FR-012**: Public failures MUST NOT expose submitted identity tokens, raw Firebase errors, service-account details, or other sensitive verification data.
- **FR-013**: Server-side verification MUST use the Firebase Admin SDK rather than trusting client-provided claims.
- **FR-014**: Firebase verification MUST support Application Default Credentials in Cloud Run.
- **FR-015**: Firebase verification MUST support the `GOOGLE_APPLICATION_CREDENTIALS` environment variable for local development.
- **FR-016**: Service-account credentials and credential file contents MUST NOT be stored in the repository.
- **FR-017**: Firebase application initialization MUST occur once per running application instance and MUST NOT be repeated for each login request.
- **FR-018**: Verification or configuration failures MUST fail closed and MUST NOT create or link users or players.

#### User Identity Resolution and Linking

- **FR-019**: The user identity MUST store Firebase UID independently from GoogleId.
- **FR-020**: FirebaseUid MUST be nullable for compatibility with existing users and unique whenever present.
- **FR-021**: User resolution MUST first look for an exact Firebase UID match.
- **FR-022**: If no Firebase UID match exists, user resolution MUST next use a verified Google provider identity from the Firebase token when available.
- **FR-023**: If neither Firebase UID nor verified Google provider identity resolves a user, resolution MAY use the Firebase email only when Firebase confirms the email is verified.
- **FR-024**: Email matching for eligible linking MUST ignore surrounding whitespace and email casing and MUST use the application's canonical normalized identity value.
- **FR-025**: An unverified email MUST NOT be used to link an existing user or legacy player.
- **FR-026**: When no user is resolved and the verified token contains all required identity information, the system MUST create exactly one active SprintLabs user linked to the Firebase UID.
- **FR-027**: When an existing user is safely resolved and has no Firebase UID, the system MUST attach the Firebase UID to that user rather than create a duplicate.
- **FR-028**: When a resolved user already has the same Firebase UID, repeated login MUST be idempotent.
- **FR-029**: When a resolved user already has a different Firebase UID, login MUST return HTTP 409 and MUST NOT replace the existing Firebase UID.
- **FR-030**: When Firebase UID, verified Google provider identity, or verified email resolve to different existing users, login MUST return HTTP 409 and MUST NOT merge or modify the accounts.
- **FR-031**: When a verified Google provider identity is present and conflicts with the resolved user's existing GoogleId, login MUST return HTTP 409 and MUST NOT overwrite either identity.
- **FR-032**: Identity-conflict checks MUST complete before committing a new external identity link, user, or player association.
- **FR-033**: Concurrent first-login or linking attempts MUST result in at most one user per Firebase UID and MUST return a safe conflict or the same resolved user for all other attempts.
- **FR-034**: User name, email, and picture returned by login MUST come from the safely resolved identity and MUST NOT silently transfer profile ownership between users.

#### Player Resolution and Defaults

- **FR-035**: Player resolution MUST first use the resolved internal UserId.
- **FR-036**: A user MUST have at most one player profile, and a player profile MUST be attached to at most one internal user.
- **FR-037**: If no player is linked by UserId, the system MUST safely locate an eligible legacy player using verified legacy identity data and attach it to the resolved user.
- **FR-038**: A legacy player already attached to another user MUST cause HTTP 409 and MUST NOT be reassigned.
- **FR-039**: Firebase UID MUST NOT be stored in `Player.GoogleId`.
- **FR-040**: The legacy player Google identity field MUST permit no value for players who do not have a Google identity.
- **FR-041**: When a verified Google provider identity exists, the legacy player Google identity MAY retain or receive that Google identity only when it does not conflict with another player.
- **FR-042**: If no eligible player exists, the system MUST create exactly one player linked by internal UserId.
- **FR-043**: New players MUST retain all current progression defaults, including Gold 0, Experience 0, Level 1, RP 0, Student rank and highest rank, TotalMatches 0, and TotalWins 0.
- **FR-044**: Repeated login or identity linking MUST preserve all existing player progression and editable profile fields.
- **FR-045**: Concurrent player resolution MUST create at most one player for the resolved internal user.

#### Existing Business Behavior

- **FR-046**: Suspended users MUST NOT receive a SprintLabs access token through Firebase login.
- **FR-047**: Eligible pending teacher memberships MUST activate after successful Firebase login under the existing invitation, membership, and seat-accounting rules.
- **FR-048**: Eligible pending student licenses MUST activate after successful Firebase login under the existing license, membership, player-linking, and seat-accounting rules.
- **FR-049**: Login-time activation MUST use the resolved internal UserId, player profile, and verified email and MUST NOT depend on Firebase UID being stored in business records.
- **FR-050**: Login-time activation MUST remain idempotent and MUST NOT create duplicate memberships, duplicate licenses, duplicate profiles, or additional seat usage.
- **FR-051**: Existing invitation eligibility, membership-role conflict, license-status, and capacity rules MUST remain unchanged.
- **FR-052**: A B2C player with no eligible invitation, membership, or license MUST log in without creating any of those records.

#### Compatibility and Profile Ownership

- **FR-053**: The existing Google login endpoint MUST remain operational with its current request, response, account reuse, player reuse, token generation, and post-login behavior.
- **FR-054**: Adding Firebase identity support MUST NOT reinterpret or overwrite existing User.GoogleId values.
- **FR-055**: Existing legacy Google users and players that never use Firebase MUST continue to log in and use player APIs.
- **FR-056**: Every authenticated player profile update path MUST locate the player using the internal `userId` access-token claim.
- **FR-057**: Profile updates MUST NOT locate the player using GoogleId, FirebaseUid, the external token subject, or the JWT subject claim.
- **FR-058**: A profile update without a valid internal `userId` claim MUST be rejected as unauthorized.
- **FR-059**: Profile updates MUST preserve existing validation, suspended-user checks, response envelopes, and current-user-only ownership rules.
- **FR-060**: The feature MUST include frontend-facing API and integration documentation for the Firebase login request, success response, 401 and 409 behavior, loading/error states, and Google-login compatibility.
- **FR-061**: The feature MUST include automated verification for first registration, repeated login, Google-provider linking, verified-email linking, unverified-email rejection, every identity conflict path, suspended users, player defaults, B2C login, teacher membership activation, student license activation, invalid tokens, internal-user profile updates, concurrency safeguards, and existing Google login behavior.
- **FR-062**: The feature MUST NOT add community membership or student-license prerequisites to ordinary player login, change progression formulas, redesign invitations or licensing, expose provider credentials, or replace the existing Google endpoint.

### Key Entities

- **User**: The internal SprintLabs identity. It owns separate nullable external identity values for Google and Firebase, has a normalized email, display details, account status, and a one-to-one relationship with a player profile.
- **Firebase Identity**: The trusted identity represented by a verified Firebase UID, project, provider data, email, email-verification status, display name, and picture. It is used only after successful server verification.
- **Player Profile**: The game profile owned through internal UserId. It stores player details and progression, may retain a legacy Google identity, and never stores Firebase UID in that Google field.
- **Community Membership**: Community access associated with an internal user. Eligible pending Teacher memberships may activate during Firebase login without changing existing invitation, role-conflict, or seat rules.
- **Student License**: A community-issued student seat assigned to an email. Eligible pending licenses may activate during Firebase login and link to the resolved internal user and player.
- **SprintLabs Access Token**: The existing application credential issued only after external identity verification and eligibility checks. Its internal userId claim is authoritative for current-player ownership.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested first-time valid Firebase logins create exactly one user and one player, and 100% of repeated logins return those same identifiers.
- **SC-002**: 100% of tested new players start with Gold 0, Experience 0, Level 1, and all other existing progression defaults.
- **SC-003**: 100% of tested B2C players without community membership or student license can complete Firebase login successfully.
- **SC-004**: 100% of tested eligible legacy Google accounts link to Firebase without creating a second user or player and without changing progression.
- **SC-005**: 100% of tested unverified-email cases avoid email-based account linking.
- **SC-006**: 100% of tested identity disagreements return HTTP 409 and leave all involved users, players, and identity links unchanged.
- **SC-007**: 100% of tested invalid, malformed, expired, revoked, incomplete, or wrong-project tokens return HTTP 401 and issue no SprintLabs access token.
- **SC-008**: 100% of tested suspended users are denied Firebase login.
- **SC-009**: 100% of tested eligible pending teacher memberships and pending student licenses reach their required active state once, with no duplicate access records or additional seat usage on repeated login.
- **SC-010**: 100% of tested successful Firebase responses use the existing login envelope and contain all nine required response fields.
- **SC-011**: Under concurrent first-login testing, each Firebase UID and each internal user end with no more than one linked user and one player profile.
- **SC-012**: 100% of tested profile updates through Google- and Firebase-issued SprintLabs tokens affect only the player associated with the internal userId claim.
- **SC-013**: All existing Google login regression tests pass without changes to the public Google login contract.
- **SC-014**: A player can complete a normal Firebase login and receive a usable SprintLabs access token within five seconds under expected service conditions, excluding a provider-wide outage.
- **SC-015**: Repository and deployment review finds zero committed service-account credentials and zero raw identity tokens or raw Firebase error details in public responses or application logs.

## Assumptions

- Firebase project configuration is supplied separately for each environment and identifies the one project whose tokens SprintLabs accepts.
- Firebase UID and a non-empty email are the required identity values for this player login. A missing display name falls back to the email for compatibility, and a missing picture returns an empty value without blocking login.
- A first-time identity with an unverified email may create a new account only when it does not match an existing normalized user or legacy player email; it may not claim existing records by email.
- Verified Google provider identity means provider data asserted by the successfully verified Firebase token, not a Google identifier supplied separately by the client.
- If the user is resolved by Firebase UID, every additional verified identity in the token is treated as a consistency check before any profile data is refreshed.
- Safe legacy player attachment uses internal UserId first and only uses verified Google provider identity or verified email as fallback evidence; ambiguous or conflicting candidates are never automatically merged.
- The new Firebase flow performs the explicitly requested pending-teacher activation while the existing Google endpoint retains its current repository behavior.
- Existing student-license activation already reserves capacity before login, so activation does not increment seat usage.
- Existing access-token lifetime, signing, envelope, and error conventions remain authoritative unless a later planning decision identifies a security defect that must be handled separately.
- Firebase provider availability and client-side Unity Firebase registration screens are dependencies; Unity UI implementation is outside this backend feature.
- Provider-specific configuration and deployment instructions belong in environment documentation and must contain references to secrets, never secret values.
