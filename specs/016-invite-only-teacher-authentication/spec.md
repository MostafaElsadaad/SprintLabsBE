# Feature Specification: Invite-Only Teacher Authentication

**Feature Branch**: 016-invite-only-teacher-authentication

**Created**: 2026-08-02

**Status**: Draft

**Input**: User description: "Replace public teacher registration and email confirmation with one coordinated invitation-only teacher authentication lifecycle, while preserving Google authentication, refresh-token rotation, logout, existing users, and single-community teacher access."

## Clarifications

### Session 2026-08-02

- Q: Can a teacher create or confirm a password account through a public registration flow? → A: No; a valid Owner-issued invitation is the only entry point for a new teacher password account.
- Q: What information can the invited teacher provide during setup, and when is the invited email confirmed? → A: The completion request contains only the invitation token, name, and password; the invitation email is immutable, and successful one-time completion confirms that email.
- Q: How does the one-community rule affect invitations? → A: A teacher may have only one Active or Pending Teacher membership; the same community must be able to resend, while a different community receives HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity.
- Q: What happens after setup and what community state permits password login? → A: Completion does not log the teacher in; later password login requires exactly one Active Teacher membership and no other Pending Teacher membership.
- Q: May this feature change Google authentication? → A: No; Google authentication behavior and tests remain unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Owner Invites One-Community Teacher (Priority: P1)

An active community Owner invites a teacher by email. The invitation reserves the teacher identity and one pending membership for that community, preventing another community from inviting or activating the same teacher while the relationship is current.

**Why this priority**: Invitation issuance is the only permitted entry point for new teacher password accounts and establishes the single-community ownership rule.

**Independent Test**: Invite a new email as an active Owner, resend from the same community, attempt an invitation from another community, and verify one identity, one pending membership, one current invitation, and no player or student records.

**Acceptance Scenarios**:

1. **Given** an active Owner and available teacher capacity, **When** the Owner invites a new normalized email, **Then** one teacher-marked identity, one pending Teacher membership, and one time-limited invitation are created and the raw invitation link is emailed.
2. **Given** a matching eligible identity without a current teacher relationship, **When** the Owner invites it, **Then** the existing identity is reused and no duplicate normalized email is created.
3. **Given** a pending invitation from the same community, **When** the Owner invites the same email again, **Then** the existing identity and membership are reused, the previous token becomes unusable, and a replacement invitation is sent without reserving another teacher seat.
4. **Given** an active or pending teacher relationship in another community, **When** an Owner attempts to invite the same teacher, **Then** the request returns HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity and neither relationship is changed.
5. **Given** a caller who is not an active Owner in the route-selected community, **When** an invitation is attempted, **Then** the request is denied and no identity, membership, invitation, or counter changes.
6. **Given** an invited teacher identity, **When** invitation creation completes, **Then** no PlayerProfile or StudentLicense exists because of the invitation.

---

### User Story 2 - Invited Teacher Validates and Completes Setup (Priority: P1)

An invited teacher opens the setup link without being signed in, safely confirms which community issued the invitation, and completes the account by choosing a name and password. Successful completion confirms the invited email and activates the existing pending membership without signing the teacher in.

**Why this priority**: This replaces public registration and authenticated invitation acceptance with a secure, one-time invitation setup journey.

**Independent Test**: Validate a current invitation, complete it with a valid name and password, verify the existing membership becomes active and the token becomes unusable, then confirm that no session or player/student data was created.

**Acceptance Scenarios**:

1. **Given** a current invitation, **When** the anonymous validation operation receives its raw token, **Then** it returns only the community name, masked invited email, and expiration time and does not activate the membership.
2. **Given** an invalid, expired, revoked, superseded, or used invitation, **When** validation is attempted, **Then** a stable safe invitation error is returned without exposing user details.
3. **Given** a valid invitation and a non-empty trimmed name plus a compliant password, **When** completion is submitted without any editable email field, **Then** the existing teacher identity receives the name and password, the immutable invited email is confirmed, its existing pending membership becomes active, and the invitation is marked used.
4. **Given** an invited identity without a username, **When** setup completes, **Then** its deterministic username is set from the normalized invited email.
5. **Given** a current active or pending relationship in another community discovered during completion, **When** completion is attempted, **Then** HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity is returned and neither relationship is changed.
6. **Given** the same invitation is completed concurrently or submitted repeatedly, **When** the requests are processed, **Then** at most one succeeds, one membership exists, and teacher usage is not incremented again.
7. **Given** successful completion, **When** the result is returned, **Then** no access or refresh credential is issued and the frontend can direct the teacher to password login.

---

### User Story 3 - Eligible Teacher Signs In to One Community (Priority: P1)

An eligible teacher signs in using either username or email and receives the existing access and refresh credentials plus the teacher's one active community.

**Why this priority**: Teachers need secure access, but password authentication must never admit unsupported, incomplete, or ambiguous community accounts.

**Independent Test**: Sign in with email and username, exercise the lockout policy, and verify that pending, removed, zero-community, multiple-current-community, suspended, and passwordless accounts receive safe failures without credentials.

**Acceptance Scenarios**:

1. **Given** an active teacher account with completed setup, a password, confirmed email, and exactly one active Teacher membership, **When** the correct email and password are submitted, **Then** the existing access and refresh credential formats and the single community are returned.
2. **Given** the same eligible account, **When** its normalized username and correct password are submitted, **Then** login succeeds with the same response shape.
3. **Given** an unknown identifier or incorrect password, **When** login is attempted, **Then** the same generic authentication error is returned and account existence is not revealed.
4. **Given** a pending invitation, passwordless identity, unconfirmed email, suspended account, or locked account, **When** teacher password login is attempted, **Then** no credentials are issued.
5. **Given** a teacher with no active community, **When** password login is attempted, **Then** login fails safely even if the password is correct.
6. **Given** inconsistent data containing multiple active Teacher memberships or multiple current teacher relationships, **When** password login is attempted, **Then** login fails safely, no community is selected, and the inconsistency is recorded without sensitive data.
7. **Given** a removed membership or an expired or revoked invitation that has not produced an active membership, **When** password login is attempted, **Then** that relationship cannot authorize login.
8. **Given** repeated invalid passwords, **When** the configured failed-attempt threshold is reached, **Then** the existing lockout duration and behavior remain enforced.
9. **Given** successful teacher login, **When** related records are inspected, **Then** no PlayerProfile or StudentLicense was created.

---

### User Story 4 - Teacher Recovers a Password Privately (Priority: P2)

An eligible teacher requests password recovery using username or email and receives a reset link without the public response revealing whether the account exists. The teacher can set a compliant password, after which all prior refresh credentials are revoked.

**Why this priority**: Invitation-only accounts still require self-service recovery that protects account privacy and compromised sessions.

**Independent Test**: Submit recovery requests for eligible and unknown identifiers, compare public responses, complete a valid reset, and verify old refresh credentials no longer rotate.

**Acceptance Scenarios**:

1. **Given** any identifier, **When** forgot-password is submitted, **Then** HTTP 202 returns "If a matching account exists, password reset instructions have been sent." regardless of account existence or eligibility.
2. **Given** an eligible completed teacher account with a password, **When** forgot-password is submitted by username or email outside the resend cooldown, **Then** a time-limited reset link is sent using the configured frontend base address.
3. **Given** an unknown, non-teacher, incomplete, passwordless, or suspended account, **When** recovery is requested, **Then** no email is sent and the public response is unchanged.
4. **Given** a valid reset link and compliant new password, **When** reset is submitted, **Then** the password changes, all active refresh credentials are revoked, and no automatic login occurs.
5. **Given** an invalid, expired, malformed, or used reset token, **When** reset is attempted, **Then** a safe invalid-or-expired error is returned and the password and memberships remain unchanged.

---

### User Story 5 - Public Registration Is Retired Without Breaking Existing Authentication (Priority: P1)

New teachers cannot create or confirm password accounts outside an Owner-issued invitation. Existing eligible teacher accounts continue to sign in, while Google authentication, refresh rotation, logout, users, and community data retain their supported behavior.

**Why this priority**: The change is incomplete if an alternate public registration route remains or unrelated authentication behavior regresses.

**Independent Test**: Verify the old registration, confirmation, and resend routes are unavailable; exercise existing eligible teacher login, Google authentication, refresh rotation, logout, and recovery; and confirm existing data is preserved.

**Acceptance Scenarios**:

1. **Given** an unauthenticated caller, **When** the retired teacher registration, confirm-email, or resend-confirmation route is requested, **Then** no public operation can create or confirm a teacher account.
2. **Given** an existing active teacher with one active community and valid password credentials, **When** password login is attempted, **Then** the account remains eligible without requiring a new invitation.
3. **Given** an existing teacher with no active community, **When** password login is attempted, **Then** access is denied without deleting or rewriting the account.
4. **Given** an existing teacher with multiple active memberships, **When** password login is attempted, **Then** access is denied safely and no community is chosen.
5. **Given** a supported Google authentication scenario, **When** Google login is exercised, **Then** its endpoint, input, validation, claims, account behavior, and response remain unchanged.
6. **Given** a valid refresh credential or logout request, **When** the existing rotation or revocation operation is used, **Then** its established behavior and credential format remain unchanged.

### Edge Cases

- Email and username matching trims surrounding whitespace and compares canonical normalized values.
- Concurrent invitations for the same normalized email from different communities result in at most one current teacher relationship; losing requests return the stable conflict.
- Concurrent same-community invitations produce one pending membership and only the newest invitation token remains usable.
- A resend that fails to deliver email does not make an older revoked invitation usable again.
- A pending membership remains a current relationship even when its invitation expires; another community cannot claim the teacher, while the same community can reissue the invitation.
- Removed memberships are historical rather than current and cannot authorize login or be activated by an old token.
- Invitation validation and completion never accept the email, user identity, community identity, or role from the frontend as authoritative.
- Completion with whitespace-only name, noncompliant password, token/email mismatch, role-changed membership, or non-pending membership changes nothing.
- Completion racing with membership removal, suspension, invitation revocation, or another invitation reissue produces no partial activation.
- Existing duplicate or multi-community teacher data is not automatically repaired; login and completion fail safely and record an inconsistency.
- Teacher seat usage is reserved once when a pending relationship is created or restored, not when it is resent or completed.
- Password reset racing with refresh rotation leaves no pre-reset refresh credential active after successful reset.
- Raw invitation, reset, access, and refresh credential values never appear in logs or persisted diagnostic data.
- Invitation and reset links remain usable after URL encoding and reject malformed transport values safely.
- Community suspension or loss of active membership prevents that relationship from satisfying teacher login eligibility.
- Incompatible player, student, Owner, administrator, or external-provider identities are not silently converted unless existing eligibility rules explicitly permit reuse as a teacher.

## Requirements *(mandatory)*

### Functional Requirements

#### Retire Public Teacher Registration

- **FR-001**: The system MUST remove or disable public teacher email registration.
- **FR-002**: The system MUST remove or disable public teacher email-confirmation and confirmation-resend operations.
- **FR-003**: No public operation MAY create, password-enable, or email-confirm a teacher identity without a valid Owner-issued invitation.
- **FR-004**: Retired operations MUST be absent from the published API description and MUST have no active request handlers reachable through public routing.
- **FR-005**: Retiring the flow MUST NOT delete or rewrite existing users, memberships, invitations, refresh credentials, player profiles, or student licenses.
- **FR-006**: Email delivery and secure account-token capabilities MUST remain available for teacher invitations and password recovery.

#### Owner Invitation

- **FR-007**: POST /api/communities/{communityId}/teachers/invite MUST remain the sole Owner teacher-invitation operation; no second invitation route MAY be introduced.
- **FR-008**: Only a caller with a current active Owner membership in the route-selected community MAY invite a teacher.
- **FR-009**: The selected community and Owner role MUST be established from backend authorization data, never from a role or community assertion supplied in the request body.
- **FR-010**: Invitation MUST normalize the submitted email through the canonical identity normalization rules.
- **FR-011**: Invitation MUST find and reuse an eligible identity with the same normalized email and MUST NOT create duplicate users for one normalized email.
- **FR-012**: A created or reused eligible identity MUST be marked as a teacher account and MUST retain incompatible-account eligibility protections already supported by the system.
- **FR-013**: An invitation-created teacher identity MUST use the normalized email as its deterministic username when no supported deterministic username already exists.
- **FR-014**: Invitation MUST NOT create a PlayerProfile or StudentLicense.
- **FR-015**: A first invitation MUST create exactly one pending Teacher membership for the invited identity and selected community.
- **FR-016**: A teacher identity MAY have at most one current teacher community relationship across all communities; current relationships include active and pending Teacher memberships.
- **FR-017**: Invitation MUST check the one-current-community rule before creating or restoring a pending membership.
- **FR-018**: Inviting a teacher with an active or pending relationship in another community MUST return HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity, MUST NOT move the teacher, and MUST NOT remove or alter the existing relationship.
- **FR-019**: Reinviting the same normalized email to the same community MUST support resending or reissuing the invitation while reusing the existing identity and membership.
- **FR-020**: Same-community reissue MUST revoke or supersede every prior usable invitation before the replacement token becomes usable.
- **FR-021**: Same-community reissue MUST NOT create duplicate pending CommunityUser or TeacherInvitation records representing simultaneous current invitations.
- **FR-022**: Creating or restoring a pending Teacher membership MUST reserve one teacher seat only when capacity is available; reissue and completion MUST NOT increment teacher usage again.
- **FR-023**: Invitation creation, membership changes, invitation supersession, and teacher-seat accounting MUST complete atomically or leave all related state unchanged.
- **FR-024**: Each issued invitation MUST record the pending membership, normalized invited email, expiration, creation time, sender, latest send or reissue time, and acceptance or revocation state.
- **FR-025**: Invitation tokens MUST be cryptographically random, time-limited, one-time-use values whose persisted representation is only a SHA-256 hash.
- **FR-026**: The raw invitation token MAY exist only transiently for link construction, delivery, validation input, and completion input; it MUST NOT be stored or logged.
- **FR-027**: Invitation email MUST send the raw link to the invited address using the configured frontend base address and MUST NOT include a password.
- **FR-028**: The invitation link format MUST support {Frontend.BaseUrl}/invitations/teacher/setup?token={urlEncodedToken} without a hardcoded frontend or backend address.
- **FR-029**: The invited email is authoritative and immutable after issuance; validation and completion MUST NOT accept an editable email field or allow the invited teacher to substitute another address.

#### Anonymous Invitation Validation and Completion

- **FR-030**: GET /api/community-invitations/teacher/validate?token={rawToken} MUST be available anonymously for the invited-teacher setup screen.
- **FR-031**: Successful validation MUST return only communityName, maskedEmail, and expiresAt.
- **FR-032**: Validation MUST NOT return the full email, raw token, password data, user identifier, membership identifier, sender details, or other unnecessary account information.
- **FR-033**: Validation MUST reject invalid, expired, revoked, superseded, and used invitations with a stable safe invitation error.
- **FR-034**: Validation MUST NOT activate or otherwise modify the pending membership.
- **FR-035**: POST /api/community-invitations/teacher/complete MUST be available anonymously, accept exactly token, name, and password as account-setup inputs, MUST NOT require or accept email or confirmPassword, and MUST replace the existing authenticated teacher invitation-acceptance route so no alternate acceptance operation can activate the membership.
- **FR-036**: Completion MUST find the invitation by the SHA-256 hash of the supplied raw token.
- **FR-037**: Completion MUST require an unexpired, unrevoked, unused invitation associated with the expected pending Teacher membership and invited identity.
- **FR-038**: Completion MUST recheck that the teacher has no other active or pending teacher relationship; a conflict MUST return HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity without changing data.
- **FR-039**: Completion MUST require a non-empty trimmed teacher name and a password satisfying the configured identity password policy.
- **FR-040**: Completion MUST set the password through the established account security mechanism and MUST NOT manipulate password representations directly.
- **FR-041**: Completion MUST set the teacher name and MUST set the normalized email as username only when a deterministic username is not already present.
- **FR-042**: Successful one-time invitation completion MUST treat possession and use of the valid invitation token as verification of the immutable invited email and mark that email confirmed; validation alone MUST NOT confirm it.
- **FR-043**: Successful completion MUST activate the existing pending CommunityUser, mark the invitation accepted or used, and MUST NOT create another CommunityUser.
- **FR-044**: Successful completion MUST NOT create a PlayerProfile, StudentLicense, access credential, or refresh credential.
- **FR-045**: Completion and all related identity, invitation, membership, and counter writes MUST be atomic.
- **FR-046**: Invitation completion MUST be one-time and replay-safe; concurrent or repeated submissions MUST activate at most one membership and MUST NOT increment teacher usage twice.
- **FR-047**: Completion MUST return a success result suitable for redirecting the teacher to login and MUST NOT automatically sign in the teacher.

#### Teacher Password Login

- **FR-048**: POST /api/account/teachers/login MUST accept identifier and password, where identifier can match normalized email or normalized username.
- **FR-049**: Identifier matching MUST be canonical and MUST NOT create distinguishable public failures for username, email, or unknown values.
- **FR-050**: Password login MUST require a teacher-marked identity with completed invitation setup or a grandfathered existing account that otherwise satisfies every current eligibility rule.
- **FR-051**: Password login MUST require a password, confirmed email, active non-suspended account, and non-locked status.
- **FR-052**: Password verification MUST preserve the configured failed-attempt and lockout policy.
- **FR-053**: Password login MUST require exactly one Active Teacher membership in an active community and zero other Pending Teacher memberships; any zero-membership, pending-only, or multiple-current-relationship state MUST fail.
- **FR-054**: Pending or removed memberships and expired, revoked, superseded, or unused invitations MUST NOT authorize password login.
- **FR-055**: A teacher with no active community MUST NOT use teacher password login.
- **FR-056**: A teacher with multiple active memberships or otherwise multiple current teacher relationships MUST fail safely; the system MUST NOT silently choose a community and MUST record the inconsistency without sensitive values.
- **FR-057**: Unknown identifier, wrong password, ineligible account, and incomplete setup MUST use a generic safe authentication failure that does not reveal whether the username or email exists.
- **FR-058**: A suspended account MUST not receive credentials; a locked account MUST remain subject to the current lockout response and duration without revealing unnecessary account details.
- **FR-059**: Successful login MUST use the existing access- and refresh-credential issuance services and formats rather than introducing a second token implementation.
- **FR-060**: Successful login MUST return userId, name, email, accountType set to Teacher, exactly one community with id and name, accessToken, accessTokenExpiresAt, refreshToken, and refreshTokenExpiresAt.
- **FR-061**: Login MUST return one community object rather than a community list or community-selection flow.
- **FR-062**: Login MUST NOT create PlayerProfile or StudentLicense records.

#### Forgot and Reset Password

- **FR-063**: POST /api/account/forgot-password MUST accept an identifier that can match normalized username or normalized email.
- **FR-064**: Forgot-password MUST always return HTTP 202 with "If a matching account exists, password reset instructions have been sent." regardless of existence or eligibility.
- **FR-065**: A reset email MAY be sent only for an active teacher password account with completed setup, confirmed email, and an existing password.
- **FR-066**: Forgot-password MUST apply the existing or configured resend/cooldown protection without exposing the cooldown or account state to an ineligible caller.
- **FR-067**: Password-reset credentials MUST use the existing secure account reset-token mechanism, remain URL safe, and MUST NOT be stored raw or logged.
- **FR-068**: Reset email links MUST use the configured frontend base address and support {Frontend.BaseUrl}/reset-password?userId={userId}&token={urlEncodedToken}.
- **FR-069**: POST /api/account/reset-password MUST accept userId, token, and newPassword and MUST enforce the configured password policy.
- **FR-070**: Reset-password MUST validate that the reset token belongs to the selected eligible teacher and is valid, unused, and unexpired.
- **FR-071**: Invalid, expired, malformed, used, or wrong-user reset tokens MUST return a safe invalid-or-expired-token error without revealing sensitive account information.
- **FR-072**: Successful reset MUST revoke all active refresh credentials for the user, MUST NOT automatically log in the user, and MUST NOT modify community membership.

#### Session and Authentication Compatibility

- **FR-073**: Existing refresh-token rotation and logout behavior, credential shape, expiration handling, one-time rotation, and revocation semantics MUST be preserved.
- **FR-074**: No second access-token, refresh-token, logout, invitation, or password-reset stack MAY be introduced when an existing service can support the required behavior.
- **FR-075**: Google authentication endpoints, handlers, inputs, outputs, claims, validation rules, account behavior, and account-linking behavior MUST remain unchanged.
- **FR-076**: Changes shared with Google authentication MUST be limited to compile-safe compatibility adjustments with no observable Google behavior change.
- **FR-077**: Player authentication, administrator authentication, community Owner authentication, PlayerProfile behavior, and StudentLicense behavior MUST remain unchanged.
- **FR-078**: Existing active teacher accounts with one active community, confirmed email, active status, password, and valid credentials MAY continue to log in without a new invitation.
- **FR-079**: Existing users MUST NOT be deleted or bulk rewritten; unsupported zero-community and inconsistent multi-community teacher accounts MUST remain stored but fail password login safely.

#### Data Integrity, Configuration, and Security

- **FR-080**: Invitation issuance and completion MUST use coordinated universal time for creation, expiration, revocation, reissue, acceptance, and related audit timestamps.
- **FR-081**: Password, invitation, reset, access, and refresh credential values MUST never be written to logs.
- **FR-082**: Duplicate submissions and invitation replay MUST not create duplicate identities, memberships, usable invitations, or teacher-seat increments.
- **FR-083**: Data protection MUST enforce unique normalized email identity lookup and unique invitation-token hashes, with efficient lookup protection for invitation and membership checks.
- **FR-084**: Database-level protection for one current teacher relationship MUST be added where it can be scoped safely to teachers without blocking supported non-teacher relationships.
- **FR-085**: If schema evolution is required, the feature MUST contain at most one non-destructive migration, preserve all existing invitation and membership data, and reuse equivalent TeacherInvitation and RefreshToken storage already present.
- **FR-086**: No migration MAY delete existing users, memberships, invitations, refresh credentials, or unrelated role data.
- **FR-087**: Email, frontend-link, access/refresh credential, password-policy, lockout, and resend settings MUST reuse existing configuration sections where possible.
- **FR-088**: No frontend or backend address MAY be hardcoded.
- **FR-089**: All community, role, membership, invitation, and account eligibility checks MUST be enforced by the backend.
- **FR-090**: Stable conflict and token errors MUST be documented, including TeacherAlreadyBelongsToAnotherCommunity and safe invalid-invitation and invalid-or-expired-reset-token errors.

#### Documentation and Verification

- **FR-091**: The published API documentation and Swagger description MUST remove obsolete public registration, confirmation, and resend-confirmation operations.
- **FR-092**: Documentation MUST describe the complete invitation-only lifecycle, login eligibility, one-community rule, password recovery, response shapes, authorization, and all new or changed error codes.
- **FR-093**: Automated verification MUST cover retired public routes; login by email and username; generic invalid credentials; pending, zero-community, multi-community, suspended, and locked login failures; successful and unauthorized invitation; same-community resend; cross-community conflict; validation success and invalid states; completion, replay, concurrency, profile/license absence, and counter safety; generic recovery; valid and invalid reset; refresh revocation; and unchanged refresh/logout behavior.
- **FR-094**: Existing Google authentication tests MUST continue to pass without behavioral changes.
- **FR-095**: Existing refresh-token and logout tests MUST continue to pass.

### Key Entities

- **Teacher Identity**: A user identity marked for teacher authentication, with normalized email and username, display name, account status, email-confirmation state, password capability, and lockout state. Invitation-created identities are incomplete until setup succeeds.
- **Community Membership**: The relationship between a user and a community, including role and Pending, Active, or Removed status. For teachers, an Active or Pending relationship is current; only one current teacher relationship may exist across communities.
- **Teacher Invitation**: A one-time, expiring invitation associated with one pending Teacher membership and immutable invited email. It retains only a non-reversible token hash plus creation, sender, reissue/send, expiration, acceptance, and revocation state.
- **Community**: The school or organization that owns the invitation and the single active relationship returned at teacher login.
- **Community License**: The capacity record whose teacher usage is reserved once when a pending membership is created or restored and is not incremented during resend or completion.
- **Refresh Credential**: The existing renewable session credential associated with a teacher identity; successful password reset revokes every active credential for that user.
- **Password Reset Credential**: A time-limited, URL-safe account recovery credential generated for an eligible teacher and never persisted or logged in raw form.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested attempts to use retired teacher registration, email-confirmation, or confirmation-resend operations create or confirm zero teacher accounts.
- **SC-002**: 100% of tested new teacher password accounts originate from a valid Owner-issued invitation and create zero PlayerProfile and StudentLicense records.
- **SC-003**: Concurrent and sequential invitations for one normalized email produce at most one current Teacher community relationship and one usable invitation token.
- **SC-004**: 100% of tested cross-community invitation and completion conflicts return HTTP 409 with TeacherAlreadyBelongsToAnotherCommunity and alter neither community relationship.
- **SC-005**: Same-community invitation reissue creates zero duplicate memberships, reserves zero additional seats, and leaves only the newest token usable.
- **SC-006**: 100% of tested valid invitation validations return only community name, masked email, and expiration; invalid, expired, revoked, and used tokens expose no user details.
- **SC-007**: Every successful invitation completion activates exactly one existing membership, confirms the email, sets a compliant password and name, increments teacher usage zero additional times, and issues no session credentials.
- **SC-008**: Concurrent or repeated completion of one invitation results in at most one success and one active membership.
- **SC-009**: 100% of eligible teacher login tests succeed with both normalized email and normalized username and return exactly one community plus the established access and refresh credential formats.
- **SC-010**: 100% of tested pending, zero-community, multi-current-community, suspended, locked, unconfirmed, passwordless, and invalid-password login attempts issue zero credentials.
- **SC-011**: Public login failures for unknown identifiers and wrong passwords are indistinguishable in status and error content.
- **SC-012**: 100% of forgot-password tests for eligible, unknown, and ineligible identifiers return the same HTTP 202 response; only eligible completed teacher accounts receive reset mail.
- **SC-013**: Every successful password reset rejects the old password, revokes all pre-reset active refresh credentials, preserves membership, and creates no automatic session.
- **SC-014**: Existing Google authentication, refresh rotation, logout, player authentication, administrator authentication, and Owner authentication verification suites pass without observable regressions.
- **SC-015**: Existing users and relationships remain present after deployment; unsupported or inconsistent teacher accounts fail safely rather than being silently reassigned or deleted.
- **SC-016**: API consumers can identify the complete invitation-only lifecycle and every stable error from the published API documentation without encountering obsolete registration operations.

## Assumptions

- The requested directory name is used exactly even though another feature already uses the numeric prefix 016.
- Existing route versioning and response-envelope conventions remain in force; route examples in this specification describe the logical paths within those conventions.
- An invitation-created account has completed setup only after the invitation is accepted, name and password are established, email is confirmed, and the associated membership is Active.
- Existing teacher accounts are grandfathered only when they satisfy the same final login eligibility rules: teacher account, active status, confirmed email, password, and exactly one active current Teacher membership.
- Active and Pending Teacher memberships count as current relationships; Removed memberships are historical and do not block a later invitation by themselves.
- An expired or revoked invitation does not by itself remove its pending membership. The pending relationship continues to reserve the teacher for the same community until explicitly removed or reissued.
- A pending membership reserves the teacher seat during initial invitation creation or restoration; completion does not consume another seat.
- A locked but otherwise eligible teacher may request password recovery; public recovery responses remain generic.
- The invited-teacher completion request contains exactly token, name, and password; email is never editable and confirm-password is not part of the backend request.
- Safe token errors intentionally avoid distinguishing unknown, expired, revoked, superseded, or already-used invitations when doing so could aid enumeration or replay attempts.
- Existing normalized-email uniqueness, TeacherInvitation storage, RefreshToken storage, email infrastructure, frontend configuration, token issuance, password policy, and lockout configuration are reused unless planning identifies a narrowly required compatibility change.
- The implementation will determine whether one non-destructive schema migration is needed after comparing existing indexes and constraints with the one-current-teacher-relationship rule.
- Delivery failure handling must not leave a falsely reported successful send or re-enable superseded tokens; exact retry mechanics are a planning decision.
- Frontend implementation, Google account-linking redesign, Google behavior changes, player or administrator authentication changes, community Owner authentication changes, multi-community teachers, player profiles, and student licenses are outside this feature.
