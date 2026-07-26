# Feature Specification: Teacher Email Authentication

**Feature Branch**: `015-teacher-email-authentication`

**Created**: 2026-07-25

**Status**: Draft

**Input**: User description: "Implement complete email/password authentication and explicit community invitation acceptance for SprintLabs teachers, including registration, confirmation, login, access and refresh credentials, password recovery, invitation email delivery, seat-safe membership activation, automated verification, and development email testing."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register and Confirm a Standalone Teacher Account (Priority: P1)

A teacher registers with a name, email address, and strong password, then confirms the email address before signing in. The teacher can complete this journey without already belonging to a community.

**Why this priority**: A verified teacher identity that is independent of community membership is the foundation for every other teacher authentication and invitation journey.

**Independent Test**: Register a new teacher using valid details, verify that no player or community records are created, follow the confirmation link, and confirm that the account becomes eligible for password sign-in.

**Acceptance Scenarios**:

1. **Given** an email address that is not in use, **When** a teacher registers with a valid name and compliant password, **Then** a teacher account is created, a confirmation email is sent, the next allowed resend time is returned, and the registration is accepted without signing the teacher in.
2. **Given** a newly registered teacher, **When** registration completes, **Then** the account has no community memberships, player profile, or student license.
3. **Given** a passwordless invited user with the same normalized email, **When** the invited teacher registers, **Then** the existing identity is reused, its name is updated, password sign-in is enabled, it is marked as a teacher account, and its pending memberships remain pending.
4. **Given** an existing Google/player identity that is not marked as a teacher account, **When** registration is attempted with the same normalized email, **Then** the request is rejected with a safe conflict response and neither a password nor a duplicate identity is created.
5. **Given** an unconfirmed teacher and a valid confirmation link, **When** the teacher confirms the email, **Then** the account becomes email-confirmed without activating any community invitation.
6. **Given** an already confirmed teacher, **When** the same valid confirmation action is repeated, **Then** the operation succeeds safely without changing unrelated account or membership data.

---

### User Story 2 - Sign In and Maintain a Teacher Session (Priority: P1)

An email-confirmed teacher signs in with email and password, receives short-lived access credentials and a renewable refresh credential, and sees all active community memberships. A teacher with no active memberships can still sign in.

**Why this priority**: Teachers need secure account access before they can accept invitations or use any authenticated teacher capability.

**Independent Test**: Confirm a teacher account with no memberships, sign in successfully, verify the empty community list, rotate the refresh credential, then sign out and verify that the submitted refresh credential can no longer be used.

**Acceptance Scenarios**:

1. **Given** an active, email-confirmed teacher account with a password, **When** the correct credentials are submitted, **Then** the response identifies the account as a teacher and includes access and refresh credentials with their expiration times.
2. **Given** an active, email-confirmed teacher with no active community memberships, **When** sign-in succeeds, **Then** the returned community list is empty.
3. **Given** an active, email-confirmed teacher with active memberships in multiple communities, **When** sign-in succeeds, **Then** every active membership is returned with community identity, community name, and role, while pending and removed memberships are excluded.
4. **Given** an unconfirmed teacher, **When** correct email and password credentials are submitted, **Then** sign-in is denied.
5. **Given** a non-teacher account, passwordless account, suspended account, locked account, or account with an incorrect password, **When** teacher sign-in is attempted, **Then** access credentials are not issued and the applicable safe error is returned.
6. **Given** five failed password attempts within the lockout window, **When** another sign-in is attempted before the lockout expires, **Then** the account remains locked for the configured duration even if the supplied password is correct.
7. **Given** a valid refresh credential for an eligible teacher, **When** it is exchanged, **Then** the old refresh credential is revoked and a new access-and-refresh pair is issued.
8. **Given** an expired, revoked, already-used, unknown, or suspended-user refresh credential, **When** refresh is attempted, **Then** no new credentials are issued.
9. **Given** a valid refresh credential, **When** the teacher signs out, **Then** that credential is revoked; repeating sign-out with the same credential is safe and does not restore or issue credentials.

---

### User Story 3 - Recover a Forgotten Password (Priority: P2)

A teacher who cannot remember the password requests a reset email and chooses a new compliant password without exposing whether any submitted email belongs to an account.

**Why this priority**: Self-service recovery prevents permanent account loss while preserving privacy and invalidating credentials that may have been compromised.

**Independent Test**: Request recovery for both existing and unknown email addresses, verify identical public responses, reset a valid teacher password, and confirm that the old password and all previously issued refresh credentials stop working.

**Acceptance Scenarios**:

1. **Given** any syntactically valid email submission, **When** password recovery is requested, **Then** the same generic success message is returned regardless of whether an eligible account exists.
2. **Given** an active teacher account with that normalized email, **When** recovery is requested, **Then** a time-limited password-reset email is sent.
3. **Given** an unknown email, a non-teacher account, or an account that is not eligible for teacher password authentication, **When** recovery is requested, **Then** no reset email is sent and the public response remains indistinguishable from the eligible-account response.
4. **Given** a valid reset link and a compliant new password, **When** the teacher resets the password, **Then** the new password works, the old password fails, and all active refresh credentials for the account are revoked.
5. **Given** an invalid, expired, previously used, or malformed reset link, **When** reset is attempted, **Then** the password remains unchanged and no active session credential is created.

---

### User Story 4 - Invite a Teacher Without Duplicating Seats (Priority: P2)

An active community Owner invites a registered or unregistered teacher by email. The invitation reserves a teacher seat once, sends an acceptance link, and leaves the membership pending until the teacher explicitly accepts.

**Why this priority**: Communities need a controlled way to recruit teachers while preserving Owner authorization, license capacity, and explicit consent.

**Independent Test**: As an active Owner, invite a new email, resend the invitation, and verify that one pending membership consumes one seat, only the newest invitation remains usable, and no automatic activation occurs.

**Acceptance Scenarios**:

1. **Given** an active community Owner and available teacher capacity, **When** the Owner invites an unregistered email, **Then** one passwordless teacher-marked identity and one pending Teacher membership are created, one teacher seat is reserved, and an invitation email is sent.
2. **Given** an existing registered teacher with no membership in the target community, **When** an active Owner invites that email, **Then** the existing identity is reused, one pending Teacher membership is created, one seat is reserved, and an invitation email is sent.
3. **Given** an existing pending Teacher membership, **When** the Owner resends or reissues its invitation, **Then** the membership and seat count remain unchanged, the previous active invitation becomes unusable, and a new invitation email is sent.
4. **Given** an active Teacher membership, **When** that teacher is invited again to the same community, **Then** no duplicate membership, invitation, or additional seat usage is created.
5. **Given** full teacher capacity, **When** an Owner attempts to create or restore a membership that would reserve another seat, **Then** the request is rejected without changing identity, membership, invitation, or seat state.
6. **Given** a user without active Owner authority in the route-selected community, **When** an invitation is attempted, **Then** the operation is denied.

---

### User Story 5 - Explicitly Accept a Community Invitation (Priority: P2)

An authenticated, email-confirmed teacher follows an invitation link and explicitly joins the invited community. Acceptance activates only the matching pending membership and does not consume another teacher seat.

**Why this priority**: Explicit acceptance prevents invitation side effects during sign-in and ensures the intended teacher controls which communities they join.

**Independent Test**: Sign in as the confirmed invited teacher, accept a valid invitation, verify that the membership becomes active with unchanged seat usage, repeat acceptance safely, and confirm that another account cannot accept it.

**Acceptance Scenarios**:

1. **Given** a valid unexpired invitation for a pending Teacher membership and the matching authenticated, email-confirmed teacher, **When** the teacher accepts, **Then** the membership becomes active, the invitation is marked accepted, and community membership information is returned.
2. **Given** acceptance activates a pending membership, **When** license usage is compared before and after acceptance, **Then** teacher usage is unchanged because the seat was reserved at invitation time.
3. **Given** the same teacher repeats acceptance after the membership is active, **When** the invitation is submitted again, **Then** the existing active membership is returned safely without duplicate membership or seat usage.
4. **Given** an authenticated account whose normalized email does not match the invited email, **When** acceptance is attempted, **Then** access is denied and the invitation and membership remain unchanged.
5. **Given** an unauthenticated, unconfirmed, non-teacher, or suspended account, **When** acceptance is attempted, **Then** the invitation is not accepted.
6. **Given** an expired, revoked, superseded, malformed, or unknown invitation, **When** acceptance is attempted, **Then** the membership remains pending and no seat or identity data changes.
7. **Given** the underlying membership is no longer pending, **When** acceptance is attempted, **Then** an already-active matching membership is returned safely, while removed or role-changed memberships are not activated.

---

### User Story 6 - Resend Email Confirmation Safely (Priority: P3)

An unconfirmed teacher requests another confirmation email after a short cooldown without the response exposing unnecessary account details.

**Why this priority**: Email delivery failures need a recovery path, but unrestricted resends create abuse and account-discovery risks.

**Independent Test**: Request confirmation twice within the cooldown and once after it, verify the returned next-available time, and compare public responses for unknown and already-confirmed accounts.

**Acceptance Scenarios**:

1. **Given** an unconfirmed teacher account whose cooldown has elapsed, **When** confirmation is resent, **Then** a new confirmation email is sent and the next allowed resend time is returned.
2. **Given** an unconfirmed teacher account still inside the cooldown, **When** resend is requested, **Then** no email is sent and the persisted next allowed resend time is returned.
3. **Given** an unknown email or already-confirmed account, **When** resend is requested, **Then** no confirmation email is sent and a safe generic response avoids unnecessary account disclosure.
4. **Given** the service restarts during the cooldown, **When** resend is requested before the stored next-available time, **Then** the cooldown remains enforced.

### Edge Cases

- Email matching trims surrounding whitespace and compares normalized values so casing cannot create duplicate identities, memberships, or invitation mismatches.
- Concurrent registration attempts for the same email result in at most one identity and do not attach a password to an ineligible Google/player account.
- Concurrent invitations for the same user and community result in at most one membership and reserve at most one teacher seat.
- Concurrent refresh attempts using the same refresh credential allow at most one successful rotation.
- Concurrent acceptance attempts for the same invitation activate the membership at most once and never increment teacher usage.
- Registration that fails before completion does not leave a partially password-enabled account falsely presented as successfully registered.
- Email delivery failure is reported as an unsuccessful operation when the caller expects a newly sent email; it does not falsely advance invitation acceptance or account confirmation.
- Confirmation, reset, and invitation links tolerate URL transport without token corruption and reject malformed values safely.
- Refresh and invitation credential values are never stored in recoverable raw form.
- A removed Teacher membership cannot be reactivated by an old invitation.
- A teacher can hold active memberships in multiple communities, and accepting one invitation does not affect memberships or invitations for any other community.
- Removing a pending or active Teacher membership releases its reserved seat once; repeating removal cannot release the seat again.
- A password reset performed near a refresh request cannot leave an older refresh credential active afterward.
- Existing Google/player sign-in remains available and does not gain teacher password sign-in through this feature.
- Community authorization continues to rely on current active membership data rather than community roles copied into sign-in credentials.

## Requirements *(mandatory)*

### Functional Requirements

#### Teacher Account Registration and Confirmation

- **FR-001**: The system MUST allow registration only for teacher password accounts using name, email, and password.
- **FR-002**: Registration MUST normalize the submitted email and enforce one identity per normalized email.
- **FR-003**: A new teacher registration MUST mark the identity as a teacher account and MUST initially require email confirmation.
- **FR-004**: Registration MUST NOT create a community membership, player profile, or student license.
- **FR-005**: When a matching passwordless invited identity exists, registration MUST reuse it, update its teacher name, add password authentication through the account security mechanism, and preserve all pending memberships.
- **FR-006**: When a matching Google/player identity is not already marked as a teacher account, registration MUST return a safe conflict and MUST NOT add a password or create a duplicate identity.
- **FR-007**: Passwords MUST contain at least eight characters and include at least one uppercase letter, one lowercase letter, one digit, and one non-alphanumeric character.
- **FR-008**: Successful registration MUST send an email-confirmation link, record the confirmation-send time, return the next allowed resend time, and complete without issuing sign-in credentials.
- **FR-009**: Email-confirmation links MUST remain valid for 24 hours by default and the lifetime MUST be configurable.
- **FR-010**: Confirmation links MUST be safe for use in a client URL and MUST bind confirmation to the intended user.
- **FR-011**: Successful email confirmation MUST NOT activate pending community memberships.
- **FR-012**: Repeating confirmation for an already-confirmed account MUST be safe and idempotent.
- **FR-013**: Confirmation resend MUST enforce a persisted 60-second cooldown by default, with a configurable cooldown duration.
- **FR-014**: Confirmation resend MUST return the next allowed resend time and MUST survive service restarts or multiple service instances.
- **FR-015**: Confirmation resend MUST NOT send mail for already-confirmed accounts and MUST use a generic public response for unknown or otherwise ineligible accounts.

#### Password Sign-In and Session Credentials

- **FR-016**: Teacher password sign-in MUST require an existing teacher-marked account with a password, confirmed email, active account status, and correct password.
- **FR-017**: Teacher password sign-in MUST NOT require community membership.
- **FR-018**: Failed password attempts MUST count toward lockout; five failed attempts MUST lock the account for 15 minutes by default, and both values MUST be configurable.
- **FR-019**: Successful sign-in MUST return teacher identity details, an access credential, a refresh credential, and the expiration time of each.
- **FR-020**: Access credentials MUST expire after 15 minutes by default, and the lifetime MUST be configurable.
- **FR-021**: Refresh credentials MUST expire after 30 days by default, and the lifetime MUST be configurable.
- **FR-022**: Issued access credentials MUST be uniquely identifiable, use coordinated universal time for validity, and be rejected when their origin, intended audience, integrity, or lifetime is invalid.
- **FR-023**: Signing-key secrets MUST come from secure configuration and MUST never be written to application logs.
- **FR-024**: Access credentials MUST NOT contain community roles; community authorization MUST continue to use current active membership data.
- **FR-025**: The sign-in response MUST include all and only active community memberships, with community identity, community name, and role.
- **FR-026**: The sign-in response MUST return an empty community list when the teacher has no active memberships.
- **FR-027**: Refresh credentials MUST be generated from a cryptographically secure source and MUST be stored only as non-reversible unique representations, never as raw values.
- **FR-028**: Each refresh credential record MUST identify its user, expiration, creation time, optional revocation time, optional replacement credential, and optional creation and revocation network addresses.
- **FR-029**: A refresh credential MUST be single-use: successful refresh MUST revoke it, issue a replacement refresh credential, and issue a new access credential.
- **FR-030**: Expired, revoked, reused, unknown, or suspended-user refresh credentials MUST NOT issue new credentials.
- **FR-031**: Concurrent refresh attempts using one credential MUST result in at most one successful rotation.
- **FR-032**: Sign-out MUST revoke the submitted refresh credential and MUST be safe when repeated.

#### Password Recovery

- **FR-033**: Password-recovery requests MUST always return the message "If an account exists, a password reset email has been sent." without revealing account existence.
- **FR-034**: The system MUST send a password-reset link only for an eligible teacher password account.
- **FR-035**: Password-reset links MUST remain valid for one hour by default, be safe for use in a client URL, and have a configurable lifetime.
- **FR-036**: A valid password reset MUST enforce the same password policy as registration and replace the old password through the account security mechanism.
- **FR-037**: Successful password reset MUST invalidate the old password and revoke every active refresh credential for that user.
- **FR-038**: Invalid, expired, malformed, or already-used reset links MUST NOT change the password.

#### Teacher Invitations and Seat Accounting

- **FR-039**: Only an authenticated user with active Owner membership in the selected community MAY invite a teacher to that community.
- **FR-040**: Invitation MUST find an existing identity by normalized email or create a passwordless placeholder identity when none exists.
- **FR-041**: Every identity created through teacher invitation MUST be marked as a teacher account; an existing eligible identity invited as a teacher MUST also be marked as a teacher account.
- **FR-042**: A new invitation MUST create or restore exactly one pending Teacher membership for the user and selected community while keeping community identity required.
- **FR-043**: Invitation MUST NOT create duplicate memberships for the same user and community.
- **FR-044**: Pending and active Teacher memberships MUST each count as one used teacher seat.
- **FR-045**: Creating or restoring a membership from a non-counted state MUST reserve one teacher seat only when capacity is available.
- **FR-046**: Resending or reissuing an invitation for an already-pending membership MUST NOT change teacher seat usage.
- **FR-047**: Accepting an invitation MUST NOT change teacher seat usage.
- **FR-048**: Removing a pending or active Teacher membership MUST release one teacher seat exactly once; repeated removal MUST NOT decrement usage again or below zero.
- **FR-049**: Each issued invitation MUST be associated with its pending membership and record invited email, expiration, creation time, inviter, latest send time, and optional acceptance or revocation time.
- **FR-050**: Invitation credentials MUST be generated from a cryptographically secure source, stored only as non-reversible unique representations, and never stored in raw form.
- **FR-051**: Invitation credentials MUST expire after seven days by default, and the lifetime MUST be configurable.
- **FR-052**: Reissuing an invitation MUST revoke any previous active invitation for that membership before the new invitation becomes usable.
- **FR-053**: Invitation email MUST contain a client link carrying the raw invitation credential; the raw value MAY appear only in the delivered link and transient response-processing memory.
- **FR-054**: Existing automatic activation of pending Teacher memberships during matching sign-in MUST be replaced by explicit invitation acceptance.

#### Invitation Acceptance

- **FR-055**: Invitation acceptance MUST require an authenticated, email-confirmed, non-suspended teacher account.
- **FR-056**: The authenticated teacher's normalized email MUST match the invitation's normalized invited email.
- **FR-057**: A valid invitation MUST be unexpired, unrevoked, and associated with a membership that is still pending with Teacher role.
- **FR-058**: Successful acceptance MUST activate the associated membership, record the acceptance time, and return community identity, community name, role, and active status.
- **FR-059**: An invitation MUST be accepted at most once, and concurrent acceptance attempts MUST produce one active membership without duplicate seat usage.
- **FR-060**: Repeating acceptance by the same matching teacher after successful activation MUST return the existing active membership safely.
- **FR-061**: A different account, including another teacher account, MUST NOT accept or consume the invitation.
- **FR-062**: Expired, revoked, superseded, unknown, or malformed invitations MUST fail without changing membership or seat state.
- **FR-063**: A removed membership or a membership whose role is no longer Teacher MUST NOT be activated by an invitation.

#### Email Delivery, Compatibility, and Verification

- **FR-064**: The system MUST support confirmation, password-reset, and community-invitation emails with configurable sender and delivery settings.
- **FR-065**: Client-facing link base addresses MUST be configurable, and email links MUST direct users to the confirmation, password-reset, or invitation-acceptance experience appropriate to the email.
- **FR-066**: Email delivery credentials MUST NOT be hardcoded or committed with application source.
- **FR-067**: Development environments MUST support a local or test SMTP inbox, including a configuration compatible with Mailpit or Mailtrap, without requiring production email-domain setup.
- **FR-068**: The existing operation for listing the current user's communities MUST continue to return all and only active memberships.
- **FR-069**: Existing Google/player sign-in, Owner/community authorization, and active-membership community access behavior MUST remain functional.
- **FR-070**: The feature MUST include automated verification for account registration, invited-user reuse, account conflict, confirmation and cooldown, sign-in and lockout, credential rotation and revocation, password recovery, invitation issuance and acceptance, seat accounting, multi-community membership, and compatibility behavior.
- **FR-071**: The feature MUST include the schema evolution needed to retain account confirmation and lockout state, refresh credentials, invitation records, teacher-account marking, and confirmation resend timing.
- **FR-072**: Production email-domain configuration, player password sign-in, platform-admin password sign-in, Google/player account linking, teacher player profiles, teacher student licenses, class assignment, frontend implementation, payment logic, parent accounts, and a global teacher authorization role MUST remain out of scope.

### Key Entities

- **User**: A person-level identity with normalized email, display name, account status, email-confirmation state, password capability, lockout state, and a marker identifying eligibility for teacher authentication. A user can exist without any community membership.
- **Community Membership**: The relationship between one user and one required community. Its role and status determine community access. Pending and active Teacher memberships reserve seats; only active memberships authorize access.
- **Refresh Credential**: A renewable, single-use session credential associated with a user. It records a non-reversible token representation, expiration, creation, revocation, replacement lineage, and optional network-origin metadata.
- **Teacher Invitation**: A time-limited, single-use invitation associated with one pending Teacher membership. It records the invited normalized email, non-reversible token representation, inviter, send time, expiry, acceptance, and revocation state.
- **Community License**: The community capacity record containing allowed and used teacher seats. Invitation, removal, resend, and acceptance rules keep usage aligned with counted memberships.
- **Confirmation Delivery State**: Persisted account-level state that records the latest confirmation send or next allowed resend time so cooldown enforcement is reliable across restarts and concurrent instances.
- **Community**: The school or organization a teacher may join. A teacher may have memberships in zero, one, or multiple communities.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of tested new-teacher registrations complete without creating a player profile, student license, or community membership.
- **SC-002**: 100% of tested eligible invited users register by reusing their existing identity, while 100% of tested ineligible Google/player conflicts create neither a password nor a duplicate identity.
- **SC-003**: 100% of tested unconfirmed teachers are denied password sign-in, and 100% of tested confirmed, active teachers with correct credentials can sign in whether they have zero or multiple communities.
- **SC-004**: Every successful sign-in returns all active memberships and no pending or removed memberships; teachers with none receive an empty list.
- **SC-005**: Access credentials cease to be accepted after the configured 15-minute lifetime, and refresh credentials cease to be accepted after the configured 30-day lifetime.
- **SC-006**: In concurrent and sequential verification, each refresh credential produces at most one successful rotation, and a signed-out credential produces zero successful refreshes.
- **SC-007**: 100% of tested password-recovery requests expose the same public response, and every successful reset invalidates the old password and all pre-reset refresh credentials.
- **SC-008**: Confirmation resend attempts made before the configured 60-second cooldown expires send zero additional emails, including after a service restart.
- **SC-009**: 100% of tested new or restored pending Teacher memberships reserve exactly one seat; resends and acceptance add zero seats; removal releases at most one seat.
- **SC-010**: 100% of tested valid invitations are accepted only by the matching confirmed teacher, while expired, revoked, superseded, malformed, or wrong-account invitations activate zero memberships.
- **SC-011**: A teacher can complete registration, email confirmation, and first successful sign-in within five minutes after receiving the required email, excluding external email-delivery delay.
- **SC-012**: Developers can observe confirmation, recovery, and invitation messages in a configured development inbox without using production email credentials.
- **SC-013**: All automated authentication, invitation, seat-accounting, and compatibility checks pass, and existing Google/player login and active community authorization remain demonstrably functional.

## Assumptions

- The current identity store remains the authoritative source for users, password state, email confirmation, security stamps, and lockout state.
- The existing teacher-management operation is updated in place; no duplicate Owner invitation operation is introduced.
- The existing teacher-management behavior that activates some invited users during invitation or matching login is superseded by this feature's explicit pending-and-acceptance workflow for password teacher invitations.
- Existing active community memberships remain active during deployment; this feature does not retroactively require already-active teachers to accept a new invitation.
- Existing pending Teacher memberships require a newly issued persisted invitation before they can be explicitly accepted.
- Email normalization uses the application's canonical identity normalization and treats surrounding whitespace and email casing as non-distinguishing.
- Safe conflict and generic account-recovery responses follow existing error-envelope conventions without revealing sensitive account details.
- Token and cooldown lifetimes use the supplied defaults unless environment configuration overrides them.
- Network-address metadata is retained when available but does not block authentication when a trustworthy client address is unavailable.
- Delivery through a local or hosted test inbox validates development configuration only; production sender-domain verification remains outside this feature.
- Frontend pages are outside scope, but the backend supplies links and response data needed for confirmation, reset, invitation acceptance, zero-community messaging, and error handling.
- No global Teacher role is introduced; the teacher-account marker controls eligibility for teacher authentication, while active community membership remains the sole source of community authorization.

