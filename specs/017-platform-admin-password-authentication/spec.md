# Feature Specification: Platform Administrator Password Authentication

**Feature Branch**: `017-platform-admin-password-authentication`

**Created**: 2026-08-05

**Status**: Draft

**Input**: User description: "Seed platform administrators through the trusted backend process with an Identity password, allow normalized username/email password login, reuse password recovery and sessions, and preserve Google authentication without behavioral changes."

> **Superseding implementation decision (2026-08-05):** Platform administrators do not have a `set-password` endpoint. They are seeded by a trusted backend process with an Identity password. All below references to first-password setup, Google-only administrator migration, or `PlatformAdminPasswordAlreadyConfigured` are superseded and must not be implemented. Community creation now receives `name` and `adminEmail`, derives the slug server-side, and issues the initial Community Admin an existing hashed-token password-setup invitation.

## Clarifications

### Session 2026-08-05

- Q: Is platform-administrator password authentication part of teacher authentication or dependent on community membership? → A: No. It is a separate feature, and platform administrators require no community.
- Q: Which credentials identify an administrator, and what authoritatively grants administrator access? → A: Login accepts username or email plus password; only persisted backend IsPlatformAdmin state grants platform-administrator access.
- Q: Who may use first-password setup, which account may it target, and what happens when a password already exists? → A: An existing Google-only platform administrator must authenticate first; setup always targets that authenticated user, while administrators with a password use forgot/reset-password.
- Q: Does this feature add any public administrator provisioning path? → A: No public administrator registration, creation, promotion, or privilege assignment is added.
- Q: Which existing authentication behavior and infrastructure must remain authoritative? → A: Google authentication and external-login records remain unchanged, and the existing JWT, refresh-token, logout, email, and Identity services are reused.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Existing Platform Administrator Signs In with a Password (Priority: P1)

An existing platform administrator who already has a password signs in with either a username or email. The administrator receives the standard access and refresh credentials needed to use existing platform-administrator operations, without needing or selecting a community.

**Why this priority**: Password authentication is the primary new capability and must authorize only identities explicitly promoted through the trusted platform-administrator process.

**Independent Test**: Sign in an eligible password-enabled platform administrator by email and username, verify the returned account type and credentials, call an existing protected administrator operation, and confirm no community, player, student, teacher, or membership data is created.

**Acceptance Scenarios**:

1. **Given** an active, password-enabled platform administrator who satisfies the current confirmed-email policy, **When** the administrator submits the correct email and password, **Then** login succeeds and returns the existing access and refresh credential formats with accountType set to PlatformAdmin.
2. **Given** the same administrator, **When** the administrator submits the correct username with different casing and surrounding whitespace plus the correct password, **Then** login succeeds for the same persisted user.
3. **Given** a successful password login, **When** the response and persisted records are inspected, **Then** no community is selected or returned and no CommunityUser, PlayerProfile, Player, StudentLicense, or teacher record is created.
4. **Given** a valid access credential from platform-administrator password login, **When** an existing platform-admin-protected operation is called, **Then** the operation recognizes the caller through the existing trusted authorization convention.
5. **Given** an unknown identifier, wrong password, non-platform-administrator identity, or passwordless platform administrator, **When** login is attempted, **Then** the same generic invalid-credentials response is returned and no credential is issued.
6. **Given** a suspended, disabled, otherwise non-active, or locked platform administrator, **When** login is attempted, **Then** no credential is issued and the existing safe account-state or lockout behavior is preserved.
7. **Given** repeated wrong passwords for an otherwise eligible platform administrator, **When** the configured failure threshold is reached, **Then** the configured lockout policy is applied; a later successful sign-in resets failed-access state according to the current authentication policy.

---

### User Story 2 - Google-Authenticated Administrator Configures a First Password (Priority: P1)

An existing Google-only platform administrator signs in through the unchanged Google flow and configures a first password for their own account. Existing sessions are invalidated for renewal, and the administrator is instructed to sign in again; Google SSO remains linked and usable.

**Why this priority**: Existing administrators may not have passwords, so this provides a secure migration path without public registration, account recreation, or loss of Google access.

**Independent Test**: Authenticate an existing passwordless platform administrator through Google, configure a compliant password, verify prior refresh credentials are revoked and no new credentials are returned, then sign in successfully by password and again through Google.

**Acceptance Scenarios**:

1. **Given** an authenticated passwordless platform administrator, **When** the caller submits a compliant new password, **Then** the password is configured for that authenticated identity, all active refresh credentials for the identity are revoked, and the response instructs the caller to sign in again.
2. **Given** an anonymous caller, **When** first-password setup is attempted, **Then** the request is denied and no account changes.
3. **Given** an authenticated non-platform-administrator, **When** first-password setup is attempted, **Then** the request is denied and no account changes.
4. **Given** an authenticated platform administrator, **When** the setup request is submitted, **Then** the target identity is derived only from the authenticated user and the request offers no target user ID, username, email, platform-admin flag, or account-type override.
5. **Given** a platform administrator who already has a password, **When** first-password setup is attempted, **Then** the request returns a stable conflict identified as PlatformAdminPasswordAlreadyConfigured and the password remains unchanged.
6. **Given** a new password that violates the configured password rules, **When** setup is attempted, **Then** safe validation details are returned and no password or refresh-credential state changes.
7. **Given** successful first-password setup, **When** the account and response are inspected, **Then** no access or refresh credential is issued, no external Google login is removed, and no community, player, student, or teacher data is created or modified.

---

### User Story 3 - Platform Administrator Recovers a Password (Priority: P2)

An eligible platform administrator uses the existing forgot-password and reset-password operations. The public forgot-password response reveals nothing about account existence or administrator status, and a successful reset revokes existing refresh credentials without automatically signing the administrator in.

**Why this priority**: Password-enabled administrators need the same secure self-service recovery as supported account types, without a parallel administrator-specific recovery flow.

**Independent Test**: Compare forgot-password responses for an eligible administrator and an unknown identifier, complete a valid reset, and verify the new password works while all pre-reset refresh credentials fail and Google SSO remains usable.

**Acceptance Scenarios**:

1. **Given** an eligible password-enabled platform administrator, **When** forgot-password is submitted using the existing identifier contract, **Then** the standard generic accepted response is returned and reset instructions are sent through the existing delivery channel and configured frontend address.
2. **Given** an unknown, non-admin, passwordless, suspended, disabled, otherwise non-active, or email-policy-ineligible identity, **When** forgot-password is submitted, **Then** the public response is identical to the eligible-account response and reveals no account details.
3. **Given** a valid reset credential for an eligible platform administrator and a compliant new password, **When** reset-password is submitted through the existing operation, **Then** the password changes, every active refresh credential is revoked, and no automatic login occurs.
4. **Given** an invalid, expired, malformed, used, or wrong-user reset credential, **When** reset-password is submitted, **Then** a safe existing reset failure is returned and the account remains unchanged.
5. **Given** a platform administrator with an existing Google login, **When** password reset succeeds, **Then** IsPlatformAdmin, Google login information, community relationships, and unrelated profile data remain unchanged.

---

### User Story 4 - Administrator Uses Shared Refresh and Logout Flows (Priority: P1)

A platform administrator authenticated by password renews and ends sessions using the same refresh and logout operations used by the current authentication system.

**Why this priority**: A login flow is not usable unless its credentials participate in the established rotation, expiration, hashing, and revocation lifecycle.

**Independent Test**: Log in by password, rotate the returned refresh credential, use the replacement access credential against an administrator-protected operation, log out with the replacement refresh credential, and verify it cannot rotate again.

**Acceptance Scenarios**:

1. **Given** a valid refresh credential issued by platform-administrator password login, **When** it is submitted to the existing refresh operation, **Then** it rotates once under the existing hashing, expiration, and revocation rules and returns a credential that still authorizes platform-administrator operations.
2. **Given** a refresh credential issued or rotated for a platform administrator, **When** it is submitted to the existing logout operation, **Then** that refresh credential is revoked and cannot be used again.
3. **Given** a revoked, expired, reused, malformed, or otherwise invalid refresh credential, **When** refresh is attempted, **Then** the existing safe refresh failure behavior is preserved.
4. **Given** a platform-administrator session, **When** its access credential is inspected, **Then** it contains the existing standard identity claims and trusted platform-administrator authorization data, and contains no community ID or community role claims.

---

### User Story 5 - Google SSO and Trusted Provisioning Remain Unchanged (Priority: P1)

Existing platform administrators continue using Google SSO before and after adding a password. No public caller can register, promote, or otherwise manufacture a platform-administrator identity through the new endpoints.

**Why this priority**: The new login method must be additive and must not weaken administrator provisioning or regress an established authentication path.

**Independent Test**: Run the existing Google authentication suite without changed expectations, add a password to an existing Google-only administrator, repeat Google login, and attempt all new operations with client-supplied administrator indicators to verify they are ignored or rejected.

**Acceptance Scenarios**:

1. **Given** any currently supported Google login scenario, **When** Google authentication is exercised, **Then** its endpoint contract, token validation, claims, account lookup or creation behavior, external-login records, JWT behavior, and response remain unchanged.
2. **Given** an existing Google-only platform administrator, **When** a first password is configured, **Then** subsequent Google login remains usable with the same behavior.
3. **Given** a non-admin user whose email, domain, username, Google identity, or request body resembles an administrator, **When** platform-admin login or password setup is attempted, **Then** platform-administrator access is not granted.
4. **Given** any unauthenticated caller, **When** the caller searches the published API surface, **Then** no public platform-administrator registration, invitation, promotion, or password-assignment operation exists.

### Edge Cases

- The identifier is null, empty, or whitespace-only; it follows the generic invalid-credentials path.
- Email and username normalize to different existing users or otherwise produce an ambiguous match; login fails generically and no user is selected.
- A platform administrator becomes suspended, disabled, non-active, demoted, or locked between credential verification and credential issuance; no new session is issued from stale eligibility data.
- A platform administrator has no email address while the current confirmed-email policy or password-recovery delivery requires one; login or recovery fails safely as applicable without revealing the reason.
- A passwordless platform administrator supplies a password to login; the result is indistinguishable from an unknown identifier, incorrect password, or non-admin user.
- Concurrent first-password setup requests target the same passwordless administrator; at most one succeeds, later requests return the already-configured conflict, and refresh credentials remain revoked.
- First-password setup races with demotion or suspension; the operation rechecks trusted current state and makes no password change for an ineligible caller.
- Password setup succeeds but a client retries because the response was lost; the retry returns the stable already-configured conflict without replacing the password.
- Password setup or reset races with refresh rotation; after the password operation succeeds, no refresh credential active before that operation can be used again.
- Reset-password succeeds for an administrator linked to Google; the external association remains present and valid.
- A platform administrator has CommunityUser or player records from historical behavior; password login neither requires, selects, returns, creates, deletes, nor modifies those records.
- Access, refresh, password-reset, and password values never appear in application logs, error payloads, or persisted diagnostic data.

## Requirements *(mandatory)*

### Functional Requirements

#### Platform Administrator Password Login

- **FR-001**: The system MUST expose POST /api/account/platform-admins/login within the repository's existing API-versioning convention.
- **FR-002**: The login request MUST accept exactly an identifier and password as authentication inputs; surrounding whitespace MUST be removed from the identifier before matching.
- **FR-003**: Identifier matching MUST support the canonical normalized username or canonical normalized email and MUST be case-insensitive through ASP.NET Core Identity normalization.
- **FR-004**: Ambiguous identifier matches MUST fail safely without selecting an account.
- **FR-005**: Login MUST require the matched persisted user to have IsPlatformAdmin set by a trusted backend process; client-supplied platform-admin or account-type values MUST never establish eligibility.
- **FR-006**: Login MUST require an existing password configured through the established account identity system.
- **FR-007**: Login MUST require the user to be active, not suspended or disabled, and not currently locked out.
- **FR-008**: Login MUST require confirmed email whenever the active application password-authentication policy requires confirmed email.
- **FR-009**: Password verification MUST use ASP.NET Core Identity password sign-in verification with failed-attempt lockout enabled and MUST preserve configured failed-attempt counting, lockout threshold, lockout duration, and successful-login reset behavior.
- **FR-010**: Unknown identifier, invalid password, non-platform-administrator user, and platform administrator without a password MUST return an indistinguishable generic invalid-credentials response with the same public status, error code, and message.
- **FR-011**: Locked-out login attempts MUST follow the existing safe lockout response without disclosing additional account information, and the possible lockout response MUST be documented.
- **FR-012**: Platform-administrator login MUST NOT require, select, create, or return a community or CommunityUser membership.
- **FR-013**: Platform-administrator login MUST NOT create or modify a PlayerProfile, Player, StudentLicense, teacher identity, teacher profile, or teacher membership.
- **FR-014**: Successful login MUST use the existing access-credential issuance service and existing refresh-credential creation infrastructure rather than a second administrator-specific implementation, and the endpoint MUST follow the repository's CQRS/MediatR, BaseResponse, and shared error-code conventions.
- **FR-015**: A successful response MUST use the existing response envelope and contain userId, name, username, email, accountType set to PlatformAdmin, accessToken, accessTokenExpiresAt, refreshToken, and refreshTokenExpiresAt.
- **FR-016**: Successful login timestamps and credential expirations MUST be expressed in coordinated universal time.

#### First Password Setup

- **FR-017**: The system MUST expose authenticated POST /api/account/platform-admins/set-password within the existing API-versioning convention.
- **FR-018**: First-password setup MUST require an authenticated caller whose current persisted identity has IsPlatformAdmin set and is otherwise eligible for authenticated administrator activity.
- **FR-019**: A platform administrator authenticated through the existing Google SSO flow MUST be allowed to use first-password setup.
- **FR-020**: The request MUST accept only newPassword as account input; the target identity MUST be resolved only from the authenticated user identifier, and the operation MUST NOT accept a target user ID, username, email, IsPlatformAdmin value, role, or account type.
- **FR-021**: First-password setup MUST require that the target platform administrator does not already have a password.
- **FR-022**: An administrator who already has a password MUST receive a stable conflict identified as PlatformAdminPasswordAlreadyConfigured, and the existing password MUST remain unchanged.
- **FR-023**: The new password MUST be configured with ASP.NET Core Identity's add-first-password operation (`UserManager.AddPasswordAsync`) and MUST satisfy the currently configured password policy; PasswordHash or any other password representation MUST never be assigned directly.
- **FR-024**: Password-policy failures MUST be returned safely in the established validation response format without echoing the submitted password.
- **FR-025**: Successful first-password setup MUST revoke all active refresh credentials belonging to the administrator; password configuration and refresh revocation MUST complete as one safe operation or report failure without leaving a successful setup response backed by renewable old sessions.
- **FR-026**: Successful first-password setup MUST issue no access or refresh credential and MUST return the message "Password configured successfully. Please sign in again."
- **FR-027**: First-password setup MUST NOT remove, replace, or alter external Google login information, and Google authentication MUST remain usable afterward.
- **FR-028**: First-password setup MUST NOT create or modify communities, CommunityUsers, PlayerProfiles, Players, StudentLicenses, teacher records, or platform-administrator privileges.
- **FR-029**: Concurrent first-password attempts MUST result in at most one successful password configuration and MUST not leave refresh credentials usable after the successful operation.
- **FR-030**: Changing a password that is already configured is outside this endpoint and MUST remain part of the normal password-reset lifecycle.

#### Forgot and Reset Password

- **FR-031**: The existing POST /api/account/forgot-password operation MUST retain its identifier request contract and MUST support eligible platform administrators in addition to every currently supported account type.
- **FR-032**: A platform administrator is eligible for forgot-password only when IsPlatformAdmin is true, a password exists, the account is active and not suspended or disabled, a deliverable email is available, and the current confirmed-email policy is satisfied.
- **FR-033**: Forgot-password MUST always return the existing generic accepted response regardless of identifier validity, account existence, account type, eligibility, email dispatch outcome, or cooldown state.
- **FR-034**: The forgot-password response for an eligible administrator MUST be indistinguishable in status, error code, envelope, message, and timing-safe observable content from the response for an unknown identifier to the extent controlled by the application.
- **FR-035**: Eligible platform-administrator recovery MUST use the existing password-reset credential provider, existing email delivery service, and configured frontend base address; no administrator-specific forgot-password flow MAY be introduced.
- **FR-036**: Raw password-reset credentials MUST NOT be persisted or logged and MUST be transported using the established safe reset-link representation.
- **FR-037**: The existing POST /api/account/reset-password operation MUST accept eligible platform administrators without adding a platform-admin-specific reset endpoint.
- **FR-038**: Platform-administrator reset MUST validate and apply the existing reset credential through ASP.NET Core Identity's established reset-password operation and MUST enforce the currently configured password policy.
- **FR-039**: Successful reset MUST revoke every active refresh credential for the administrator as part of the safe password-reset operation and MUST NOT automatically sign the administrator in or issue credentials.
- **FR-040**: Successful or failed reset MUST NOT change IsPlatformAdmin, remove Google external-login information, or create, delete, select, or modify community memberships.
- **FR-041**: Existing forgot-password and reset-password behavior for all previously supported account types MUST remain supported unless separately changed by another feature.

#### Refresh, Logout, JWT, and Authorization

- **FR-042**: Access and refresh credentials issued by platform-administrator password login MUST work with the existing refresh-token operation and its current hashing, single-use rotation, replacement, expiration, and revocation rules.
- **FR-043**: Platform-administrator refresh MUST recheck current trusted eligibility so a suspended, disabled, non-active, or demoted administrator cannot renew a session.
- **FR-044**: Platform-administrator refresh MUST preserve the administrator authorization data needed by existing protected operations and MUST NOT silently convert the session to a teacher, player, or community account.
- **FR-045**: Platform-administrator refresh credentials MUST work with the existing logout operation and revocation behavior; no administrator-specific refresh or logout operation MAY be introduced.
- **FR-046**: Administrator access credentials MUST contain the authenticated user identifier and the existing standard claims required by the application.
- **FR-047**: Platform-administrator authorization data in an access credential MUST be derived exclusively from persisted trusted account state and expressed through the current trusted claim or authorization convention.
- **FR-048**: Administrator access credentials MUST NOT contain community IDs or community roles solely because the user is a platform administrator.
- **FR-049**: Existing platform-admin-protected operations MUST accept valid credentials issued or rotated from the new password-login flow without weakening their authorization checks.
- **FR-050**: The accountType returned to the client MUST be informational and MUST NOT be sufficient by itself to authorize platform-administrator access.
- **FR-051**: No second access-token, refresh-token, reset-token, or logout implementation MAY be introduced when the existing shared flow can be extended safely.

#### Google Compatibility and Trusted Provisioning

- **FR-052**: Existing Google login endpoint contracts, token validation, claims, account creation or lookup behavior, external-login records, credential behavior, responses, and tests MUST remain unchanged.
- **FR-053**: Shared changes required for platform-administrator password authentication MUST be limited to compile-safe compatibility changes with no observable Google authentication behavior change.
- **FR-054**: Adding or resetting a platform-administrator password MUST NOT disconnect, overwrite, or remove Google SSO linkage.
- **FR-055**: The feature MUST NOT add public platform-administrator registration, invitation, creation, promotion, or privilege-assignment capabilities.
- **FR-056**: The system MUST NOT grant platform-administrator status based on email address, email domain, username, Google identity, request body values, community ownership, teacher status, or any other client-controlled assertion.
- **FR-057**: Existing platform-administrator users and privileges MUST be preserved without deletion, recreation, bulk rewrite, or forced password replacement.
- **FR-058**: Platform-administrator password authentication MUST remain separate from teacher authentication; existing non-admin users, community owners, and teachers MUST NOT become eligible for platform-administrator password login unless separately promoted through the existing trusted internal process, and this feature MUST NOT change teacher authentication behavior.

#### Security, Persistence, Documentation, and Verification

- **FR-059**: Passwords and raw access, refresh, and password-reset credentials MUST never be logged, returned in error details, or stored in diagnostic data.
- **FR-060**: Password values MUST be handled only through ASP.NET Core Identity APIs; stored password representations MUST never be manipulated directly, and no default, shared, or generated administrator password MAY be introduced.
- **FR-061**: Account eligibility MUST be rechecked closely enough to credential or password issuance to prevent stale trusted-state decisions from granting access or configuring a password after suspension, disabling, or demotion.
- **FR-062**: Existing identity fields for normalized username, normalized email, password, confirmed email, failed attempts, lockout, account status, and IsPlatformAdmin MUST be reused; no duplicate administrator, password, or credential tables MAY be added.
- **FR-063**: A database migration MAY be created only when inspection during planning proves the existing schema cannot support a required behavior.
- **FR-064**: Any required migration MUST be non-destructive and preserve existing users, passwords, administrator privileges, external Google logins, refresh credentials, and community relationships.
- **FR-065**: Published API documentation and Swagger MUST add platform-administrator password login and authenticated first-password setup while retaining the existing Google login operation.
- **FR-066**: Documentation for login MUST show the request and successful response, generic invalid-credentials behavior, possible safe lockout response, absence of community selection, and shared refresh/logout compatibility.
- **FR-067**: Documentation for first-password setup MUST show authentication and platform-admin requirements, the newPassword request, password-policy failures, PlatformAdminPasswordAlreadyConfigured conflict, refresh revocation, no-token success response, and continued Google compatibility.
- **FR-068**: Frontend-facing API documentation MUST be created or updated for this feature, while frontend implementation remains out of scope.
- **FR-069**: Automated verification MUST cover email and username login; trimming, normalization, and case-insensitivity; response account type; absence of community requirements and side effects; generic failures; account-status and lockout behavior; credential issuance; authorization; rotation; and logout.
- **FR-070**: Automated verification MUST cover authenticated first-password setup through Google, anonymous and non-admin rejection, self-only targeting, password-policy validation, concurrency, already-configured conflict, refresh revocation, no token issuance, and preserved Google access.
- **FR-071**: Automated verification MUST cover indistinguishable forgot-password responses, successful administrator reset, reset-time refresh revocation, unchanged privilege and membership data, preserved Google linkage, and no automatic login.
- **FR-072**: Existing Google authentication tests MUST pass without changed expectations, and existing authentication tests for previously supported account types MUST remain passing.

#### Layering and Delivery Constraints

- **FR-073**: Delivery MUST preserve the existing dependency direction: API to Application and Infrastructure, Application to Domain, Infrastructure to Domain, Domain to Shared, and Shared to no other project.
- **FR-074**: Application MUST NOT reference Infrastructure, Infrastructure MUST NOT reference Application, Domain MUST contain no infrastructure service implementation, and Shared MUST remain independent.
- **FR-075**: Any service contract required across layers MUST remain in Domain/Services, any implementation MUST remain in Infrastructure/Services, shared cross-layer results MUST remain in Shared, and infrastructure service registration MUST remain in Infrastructure/ServiceConfig.cs.
- **FR-076**: The API surface MUST remain thin and dispatch the feature through the existing CQRS/MediatR vertical-slice conventions rather than embedding authentication behavior in the controller.
- **FR-077**: Normal persistence and eligibility lookups MUST reuse the repository's established identity, database context, and base-repository patterns; a feature-specific repository MAY be added only if planning proves the existing abstractions cannot support a required complex or reusable operation cleanly.
- **FR-078**: The feature MUST modify the fewest existing shared authentication components needed to support platform administrators and MUST NOT add packages, project references, generic authentication frameworks, or parallel token/password services.
- **FR-079**: Implementation delivery MUST include the repository-required `api.md` and `frontend.md` for this backend API feature; frontend.md MUST describe integration states and permissions without adding frontend implementation to scope.

### Key Entities

- **Platform Administrator Identity**: An existing user identity whose persisted trusted IsPlatformAdmin state grants platform-level administration. Relevant state includes normalized username and email, display name, email confirmation, password capability, account status, failed-access count, and lockout state; it has no required community relationship.
- **External Google Login Association**: The existing linkage that permits Google SSO for an identity. Adding or resetting a password must leave this association and its supported behavior unchanged.
- **Access Credential**: The existing short-lived session credential containing standard identity claims and trusted platform-administrator authorization data, with no required community claims.
- **Refresh Credential**: The existing renewable session credential associated with a user identity and governed by current hashing, expiration, single-use rotation, replacement, and revocation rules.
- **Password Reset Credential**: The existing time-limited recovery value delivered to an eligible account and never stored or logged in raw form.
- **Account Security Policy**: The currently configured password, confirmed-email, failed-attempt, and lockout rules that apply consistently to platform-administrator password operations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of eligible platform-administrator test accounts can sign in with both their normalized email and normalized username, including case variations and surrounding identifier whitespace.
- **SC-002**: 100% of successful platform-administrator password logins return accountType PlatformAdmin, standard access and refresh credentials with UTC expirations, and zero community identifiers or community selections.
- **SC-003**: 100% of tested unknown identifiers, wrong passwords, non-admin users, and passwordless administrators receive the same public invalid-credentials status, error code, and message and receive zero credentials.
- **SC-004**: 100% of tested suspended, disabled, non-active, or locked administrator login attempts receive zero credentials; configured failed-attempt and lockout behavior remains effective.
- **SC-005**: Successful platform-administrator login, first-password setup, recovery, refresh, and logout create zero CommunityUser, PlayerProfile, Player, StudentLicense, or teacher records.
- **SC-006**: Every tested access credential from platform-administrator password login or refresh authorizes the same existing administrator-protected operations as a currently supported administrator session and authorizes no operation based solely on the response accountType.
- **SC-007**: 100% of tested refresh credentials rotate at most once under existing expiration and revocation rules, and a logged-out or superseded credential cannot rotate again.
- **SC-008**: Every successful first-password setup targets only the authenticated passwordless administrator, revokes all active refresh credentials, returns no new credential, and instructs the administrator to sign in again.
- **SC-009**: Concurrent first-password submissions configure at most one password; all later attempts return PlatformAdminPasswordAlreadyConfigured without replacing the configured password.
- **SC-010**: 100% of forgot-password requests for eligible administrators and unknown identifiers return the same accepted response; only eligible accounts enter the existing reset-delivery flow.
- **SC-011**: Every successful platform-administrator password reset rejects the previous password, revokes all pre-reset refresh credentials, issues no automatic session, and preserves administrator privilege, Google linkage, and community data.
- **SC-012**: Existing Google SSO scenarios pass their unchanged verification suite before and after an administrator adds or resets a password.
- **SC-013**: 100% of attempts to obtain or assign platform-administrator access through email, domain, username, Google identity, community role, teacher role, or request body values fail unless persisted trusted IsPlatformAdmin state is already present.
- **SC-014**: Existing users, external login records, administrator privileges, and unrelated relationships remain present and unchanged after deployment or any required schema update.
- **SC-015**: API consumers can identify login, first-password setup, recovery, refresh, logout, authentication requirements, safe errors, and Google compatibility from the published API documentation without finding a public administrator-registration path.

## Assumptions

- The logical routes in this specification are published within the repository's existing URL versioning convention even where the request lists unversioned examples.
- The current account-status model has Active and Suspended states; any later disabled or equivalent non-active state is treated as ineligible without requiring a separate feature behavior.
- The confirmed-email requirement is policy-driven. It is currently enabled, but this feature follows the active policy rather than hardcoding a second independent rule.
- A platform administrator does not need a CommunityUser relationship, and any historical community or player data attached to an administrator is ignored and left unchanged by these operations.
- The current trusted platform-administrator authorization convention may be a claim, policy, or protected-operation lookup; planning will identify and reuse it rather than inventing authorization based on the response accountType.
- Existing access, refresh, logout, password-recovery, email, frontend-base-address, password-policy, lockout, and response-envelope capabilities are extended in place.
- A locked administrator remains ineligible for password login. Forgot-password eligibility follows the current recovery policy where lockout alone does not disclose account existence or bypass reset-token validation.
- Revoking refresh credentials after password setup or reset does not imply minting a replacement access credential; the caller must authenticate again.
- Existing Google-only platform administrators are already provisioned through a trusted internal process and can authenticate through Google before calling first-password setup.
- No schema change is expected for normalized identifiers, password state, email confirmation, lockout, account status, or IsPlatformAdmin; planning may add one narrowly required non-destructive migration only if inspection finds a real gap.
- Frontend implementation, public administrator registration, administrator invitation or promotion, administrator MFA, Google linking redesign, teacher login changes, player login changes, community-owner authentication changes, and password change for already-password-enabled administrators are outside this feature.
