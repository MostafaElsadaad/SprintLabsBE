# Feature Specification: Production-Like Development Player Authentication

**Feature Branch**: `codex/020-production-like-dev-auth`

**Created**: 2026-09-02

**Status**: Approved

**Input**: User description: "Allow Unity Editor developers to select one of eight deterministic, database-backed development players and receive a real SprintLabs player login result and access token without Google or Firebase, while keeping all downstream backend and Mirror authentication behavior identical to production."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ensure Deterministic Development Players (Priority: P1)

A backend developer explicitly enables development-player seeding in a non-Production environment and starts the backend. Eight known development accounts become available as real, active account and player-profile records. Repeated startup keeps the same identities and repairs compatible seeded values without creating duplicates.

**Why this priority**: Stable real identities are the foundation for account selection, login, player-profile testing, and trusted Mirror authentication. No development login is valid unless these identities already exist in the backend.

**Independent Test**: Enable development authentication and player seeding in a Development environment, start the backend repeatedly, and verify that exactly eight canonical account keys resolve to eight distinct, stable account/profile pairs with no duplicate records.

**Acceptance Scenarios**:

1. **Given** a Development environment, development authentication enabled, player seeding enabled, and no development players, **When** startup completes, **Then** exactly `dev-player-01` through `dev-player-08` exist as eight real active accounts with eight linked real player profiles and display names `Dev Player 01` through `Dev Player 08`.
2. **Given** the eight seeded players already exist, **When** the enabled Development startup runs again, **Then** the same account and profile identifiers remain associated with each account key and no duplicate account or profile is created.
3. **Given** a compatible seeded account or profile has a missing or stale seed-owned value, **When** seeding runs, **Then** the intended deterministic value is reconciled without changing the record's identity.
4. **Given** an account key or deterministic identity collides with incompatible existing data, **When** seeding runs, **Then** startup reports an actionable safe failure rather than taking over the record, changing its identity, or creating a duplicate.
5. **Given** seeding is interrupted after only some players are ensured, **When** a later enabled Development startup runs, **Then** the complete set is reconciled without duplicating the previously completed players.

---

### User Story 2 - Log In as a Seeded Player with a Real SprintLabs Token (Priority: P1)

A Unity Editor developer submits a selected development account key and receives the same player login contract used by production login. The returned access token is a normal SprintLabs token that passes the existing authenticated-current-user flow and resolves to the same real account and player-profile identifiers.

**Why this priority**: This is the primary user outcome. It replaces only the external identity-provider step while preserving every trusted backend and Mirror authentication step after login.

**Independent Test**: Submit `dev-player-01` to the development login operation, use the returned bearer token with `GET /api/v1/Users/me`, and verify that both responses identify the same persisted account and player profile.

**Acceptance Scenarios**:

1. **Given** enabled development authentication in a non-Production environment and an existing seeded `dev-player-01`, **When** the account key is submitted to `POST /api/v1/Account/development-login`, **Then** the response succeeds using the normal `LoginResponse` contract.
2. **Given** a successful development login, **When** the response is inspected, **Then** it contains a normal SprintLabs access token plus the real `UserId`, `PlayerProfileId`, name, email, picture URL, gold, experience, and level associated with the selected persisted player.
3. **Given** a token returned by development login, **When** it is presented to `GET /api/v1/Users/me`, **Then** normal bearer-token validation succeeds and returns the same `UserId` and `PlayerProfileId` as the login response.
4. **Given** repeated successful login for `dev-player-01`, **When** the responses are compared, **Then** the same persisted `UserId` and `PlayerProfileId` are returned even though individual token values may differ.
5. **Given** successful logins for `dev-player-01` and `dev-player-02`, **When** their identities are compared, **Then** they have different `UserId` and `PlayerProfileId` values.
6. **Given** an unknown or unseeded account key, **When** development login is attempted, **Then** the request is rejected and no account or profile is created.
7. **Given** a seeded account that is no longer in a valid active state or whose linked player profile is missing or incompatible, **When** development login is attempted, **Then** login is rejected without issuing a token or repairing the account lazily.

---

### User Story 3 - Prevent Development Authentication Outside Explicit Safe Conditions (Priority: P1)

An operator can rely on development authentication being off by default and impossible to use or seed in Production. An optional independently configured development authorization key protects both development operations when a non-Production backend is reachable beyond a trusted local machine.

**Why this priority**: A development login path that can accidentally run in Production or expose seeded accounts without intended authorization would be a critical security failure.

**Independent Test**: Exercise startup, account discovery, and development login across disabled, enabled Development, invalid-key, and Production configurations and verify that only the explicitly enabled, authorized, non-Production combination succeeds.

**Acceptance Scenarios**:

1. **Given** development authentication configuration is absent or disabled, **When** startup and both development operations are exercised, **Then** no development players are seeded and neither operation exposes or authenticates development accounts.
2. **Given** the runtime environment is Production, **When** development authentication and seeding settings are mistakenly enabled, **Then** no development players are seeded and both development operations remain unavailable.
3. **Given** development authentication is enabled in a non-Production environment but player seeding is disabled, **When** startup occurs, **Then** no development account is created or reconciled.
4. **Given** a development authorization key is configured, **When** either development operation is called without the key or with an invalid key, **Then** the request is rejected without returning account data or a player token.
5. **Given** a development authorization key is configured, **When** a valid key is supplied to an enabled development operation in a non-Production environment, **Then** key authorization succeeds and normal operation-specific validation continues.
6. **Given** no development authorization key is configured, **When** development authentication is explicitly enabled in a non-Production environment, **Then** the operations follow the configured key-optional behavior without inventing or exposing a secret.

---

### User Story 4 - Discover Safe Development Account Choices (Priority: P2)

A Unity Editor developer can request the available seeded development players and present them as deterministic account choices without receiving credentials, tokens, or internal database information.

**Why this priority**: Discovery makes the later Unity Editor selector practical, but login remains possible with a known account key even before an editor UI exists.

**Independent Test**: Request `GET /api/v1/Account/development-players` under valid development conditions and verify that the response contains exactly the eight safe account choices and no secret or identity-provider material.

**Acceptance Scenarios**:

1. **Given** enabled development authentication in a non-Production environment with all seeded players present, **When** the development-player list is requested, **Then** exactly eight entries are returned in account-key order from `dev-player-01` through `dev-player-08`.
2. **Given** a returned development-player entry, **When** it is inspected, **Then** it contains at least `accountKey` and `displayName`; level and rank tier may be included only when sourced from the persisted player profile.
3. **Given** the full list response, **When** all response fields are inspected, **Then** it contains no access token, password, development authorization key, Google token, Firebase token, JWT, connection string, database identifier, or secret value.
4. **Given** a seeded account/profile pair is missing or invalid, **When** discovery is requested, **Then** the endpoint does not present that entry as a usable account and reports the invalid seeded state safely rather than creating or repairing it during the request.

---

### User Story 5 - Preserve Production Authentication Compatibility (Priority: P2)

Existing Firebase and Google player login continue to behave as before. From the moment a development login response is returned, consumers use the same session, bearer-token, current-user, and Mirror validation behavior as production players, with no development-specific downstream identity path.

**Why this priority**: The feature must speed up Editor development without weakening or forking the authentication model that protects production identity.

**Independent Test**: Run the existing Firebase-login verification alongside a development login, compare their response contracts and token validation behavior, and verify no change is required to current-user or Mirror authentication.

**Acceptance Scenarios**:

1. **Given** existing valid Firebase player-login input, **When** Firebase login is performed after this feature, **Then** its established authentication, registration/linking, token, and response behavior remains unchanged.
2. **Given** either a production player login or a development player login, **When** the returned access token is validated by the existing protected current-user operation, **Then** the same trusted identity rules and response contract apply.
3. **Given** a development login response, **When** a downstream consumer handles it, **Then** no fake token, fake identifier, alternate token format, development-only identity claim, or Mirror bypass is required.

### Edge Cases

- Two startup processes attempt to ensure the same deterministic players concurrently; the final state contains one account and one linked profile per account key, or one process fails safely without duplicate identities.
- A seeded account exists but its player profile does not, belongs to another account, or conflicts by deterministic email/key. Login rejects the invalid relationship and seeding reconciles only cases explicitly safe to repair.
- A deterministic profile exists without its intended account relationship. Seeding must not silently attach it to an unrelated account.
- A seeded account is suspended, inactive, locked, or otherwise invalid under normal player-account rules. Development login does not override that status.
- An account key differs by case, contains leading/trailing whitespace, or is outside `dev-player-01` through `dev-player-08`. Only the canonical seeded key is accepted; ambiguous normalization does not select an account.
- The configured authorization key is empty or whitespace. It is treated as not configured rather than as a valid empty credential.
- The authorization header appears more than once. The request is rejected rather than choosing one value.
- Production configuration mistakenly contains both enablement and a valid development key. The Production environment restriction still wins.
- A development-login request is replayed. It may issue a new normal token but always resolves the same persisted identity and does not create or mutate an account.
- Sample progression is seeded. Experience/level and RP/rank values remain mutually consistent with existing progression rules; development login does not award or recalculate progression.
- Logs are collected at verbose levels. Tokens, authorization keys, passwords, identity-provider tokens, connection strings, and database credentials remain absent.

## Requirements *(mandatory)*

### Functional Requirements

#### Configuration and Environment Safety

- **FR-001**: Development player authentication MUST be disabled by default.
- **FR-002**: Development player seeding MUST be disabled by default and MUST require its own explicit opt-in in addition to development authentication being enabled.
- **FR-003**: The runtime Production environment MUST unconditionally prevent development-player seeding, account discovery, and development login even when configuration mistakenly enables them.
- **FR-004**: Development configuration MUST distinguish at least overall enablement, seed enablement, and an optional authorization key.
- **FR-005**: No development authorization key, database password, connection string, player token, identity-provider token, or other secret MAY be committed as a usable value in repository configuration or source files.
- **FR-006**: Local secrets MUST be supplied through developer-secret or environment configuration, and deployed secrets MUST be supplied through deployment-managed secret configuration.
- **FR-007**: When the optional authorization key is configured, both development account discovery and development login MUST require the caller to supply the matching key through `X-SprintLabs-Dev-Key`.
- **FR-008**: A missing, malformed, duplicated, or invalid configured development key MUST reject the request without revealing the expected key, returning account data, or issuing a token.
- **FR-009**: The development authorization key MUST be treated only as authorization to call development operations; it MUST NOT identify a player, become a player access token, or be embedded in a player token.

#### Deterministic Development Players

- **FR-010**: Enabled non-Production seeding MUST create or reconcile exactly eight canonical development account keys: `dev-player-01`, `dev-player-02`, `dev-player-03`, `dev-player-04`, `dev-player-05`, `dev-player-06`, `dev-player-07`, and `dev-player-08`.
- **FR-011**: The canonical display names MUST be `Dev Player 01` through `Dev Player 08`, corresponding to the account-key suffix.
- **FR-012**: Each development account key MUST resolve to one real persisted account and one real persisted player profile linked through the normal account/profile relationship.
- **FR-013**: Account and player-profile identifiers MUST use the backend's authoritative 64-bit identity type and MUST NOT be synthesized from account keys or restricted to 32-bit values.
- **FR-014**: Seeded accounts and player profiles MUST be active and valid under the same account-state checks used for normal player login.
- **FR-015**: Development account seeding MUST run through the backend's normal application startup, identity, and persistence boundaries; it MUST NOT depend on a manual database connection script, embedded database credentials, a second persistence context, or startup-time calls to the development-login operation.
- **FR-016**: Seeding MUST be idempotent: repeated or interrupted runs MUST preserve each existing account and profile identifier and MUST create no duplicate account, profile, key, email, or relationship.
- **FR-017**: Seeding MUST use stable deterministic business identifiers to locate intended accounts and MUST reject incompatible collisions instead of overwriting unrelated identities or privileges.
- **FR-018**: Development accounts MUST be ensured before they can be used; account discovery and login MUST NOT lazily create, repair, or attach accounts or profiles.
- **FR-019**: Seeded profile values MUST be deterministic. Default progression values MAY be used for all eight accounts.
- **FR-020**: If varied sample progression is included, every Experience/Level and RP/RankTier combination MUST agree with existing progression rules, and the feature MUST NOT award progression or add a progression-processing workflow.

#### Development Account Discovery

- **FR-021**: The backend MUST expose `GET /api/v1/Account/development-players` only when development authentication is enabled and the environment is not Production.
- **FR-022**: Discovery MUST apply the configured development authorization-key requirement before returning any account information.
- **FR-023**: A successful discovery response MUST return only valid existing seeded accounts, ordered by canonical account key.
- **FR-024**: Every discovery item MUST contain `accountKey` and `displayName`; it MAY additionally contain persisted `level` and `rankTier` when those fields are available without changing progression.
- **FR-025**: Discovery MUST NOT return access tokens, JWTs, passwords, authorization keys, Google/Firebase tokens, account-provider secrets, connection strings, database details, or any secret configuration.
- **FR-026**: Disabled, unauthorized, invalid-seed-state, and Production discovery requests MUST fail without exposing the available account set.

#### Development Login

- **FR-027**: The backend MUST expose `POST /api/v1/Account/development-login` only when development authentication is enabled and the environment is not Production.
- **FR-028**: Development login MUST accept one canonical `accountKey` and MUST reject missing, malformed, unknown, non-canonical, or unseeded keys.
- **FR-029**: Development login MUST apply the configured development authorization-key requirement before resolving or authenticating a development player.
- **FR-030**: Development login MUST find the existing seeded account and linked player profile; it MUST NOT create an account, create a profile, manufacture identifiers, or repair invalid relationships during login.
- **FR-031**: Development login MUST enforce normal account validity and suspension rules before issuing a token.
- **FR-032**: Development login MUST issue a real SprintLabs player access token through the existing player-token issuance behavior used by production external player login.
- **FR-033**: The development token MUST use the same issuer, audience, signature validation, lifetime rules, and trusted `userId` and `playerProfileId` identity claims expected by existing bearer-token consumers.
- **FR-034**: Development login MUST NOT issue a fake token, use a development-only token format, accept the development authorization key as a player credential, invoke Google/Firebase, or bypass normal token validation.
- **FR-035**: Development login MUST return the existing normal `LoginResponse` contract rather than a development-specific response contract.
- **FR-036**: The login response MUST populate `accessToken`, `userId`, `playerProfileId`, `name`, `email`, `pictureUrl`, `gold`, `experience`, and `level` from the real authenticated account and player profile using the same meanings as production login.
- **FR-037**: A token returned by development login MUST successfully authenticate `GET /api/v1/Users/me` without any change or exception in that operation.
- **FR-038**: The `UserId` and `PlayerProfileId` returned by `GET /api/v1/Users/me` MUST equal the identifiers returned by the development login that issued the token.
- **FR-039**: Repeated login for one account key MUST return the same persisted account and profile identifiers, while different canonical keys MUST resolve to different identities.
- **FR-040**: Unknown-account and invalid-account requests MUST return a controlled rejection and MUST leave account/profile record counts unchanged.

#### Compatibility, Observability, Validation, and Scope

- **FR-041**: Existing Firebase and Google player-login behavior, response compatibility, account linking/creation behavior, and token validation MUST remain unchanged.
- **FR-042**: Existing `GET /api/v1/Users/me` authentication, authorization, and trusted identity response MUST remain unchanged.
- **FR-043**: No Unity client, Unity Editor UI, SprintLabs API client, Mirror authenticator, or dedicated-server identity-validation change is part of this feature.
- **FR-044**: Successful and failed development login MAY log the canonical account key and resolved `UserId`/`PlayerProfileId`; seeding MAY log whether it ran and the count of accounts ensured.
- **FR-045**: Logs MUST never contain access tokens, development authorization keys, database connection strings, passwords, Google/Firebase tokens, or other secret values.
- **FR-046**: Focused validation MUST cover first-run seeding, repeated-run idempotency, stable identity, distinct identities, safe discovery, unknown-account rejection, token validity, current-user identity equality, Production refusal, disabled-configuration refusal, invalid-key rejection, and existing Firebase-login compatibility.
- **FR-047**: The feature MUST remain limited to development-player configuration, deterministic seeded account/profile records, safe development account discovery, development login, reuse of existing player JWT issuance, focused tests, and required feature documentation.
- **FR-048**: Player-profile API extensions, rank/profile UI, match completion, match/question history, progression awarding, XP/RP processing, missions, telemetry, general authentication refactoring, new generic dependency frameworks, and unrelated backend cleanup MUST remain out of scope.

### Key Entities *(include if feature involves data)*

- **Development Player Account**: A logical deterministic development identity selected by canonical account key. It is backed by one real account and one real linked player profile; it is not a separate fake identity or token type.
- **User Account**: The existing authoritative account identity with a real 64-bit identifier and normal status/validity rules. A development player uses the same account model as a production player.
- **Player Profile**: The existing gameplay profile linked one-to-one with the account and containing display name, profile fields, gold, experience, level, RP, and rank state.
- **Development Authentication Configuration**: Non-secret enablement and seed switches plus an optional secret authorization value supplied outside source control. The Production environment restriction overrides all switches.
- **Player Login Result**: The existing production-compatible login response containing a real SprintLabs bearer token and the persisted account/profile identity and profile snapshot.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: One enabled non-Production seed run produces exactly eight selectable development players, each with one real account, one linked real player profile, and a unique account/profile identity pair.
- **SC-002**: Running development-player seeding at least three times leaves exactly eight accounts and eight linked profiles, with 100% of account and profile identifiers unchanged after the first successful run.
- **SC-003**: Across at least 100 repeated logins for one canonical account key, 100% return the same persisted `UserId` and `PlayerProfileId`, and zero additional accounts or profiles are created.
- **SC-004**: The eight canonical account keys resolve to eight distinct `UserId` values and eight distinct `PlayerProfileId` values with no cross-linked pair.
- **SC-005**: Every successful development login returns all fields in the normal player login contract, and its token successfully completes the existing current-user authentication flow on the first attempt.
- **SC-006**: In 100% of successful login/current-user comparisons, both operations return identical `UserId` and `PlayerProfileId` values.
- **SC-007**: Unknown account keys, invalid account states, missing profiles, and incompatible relationships create zero new accounts/profiles and issue zero tokens.
- **SC-008**: A valid discovery request returns exactly the usable seeded accounts in deterministic order and exposes zero secret, token, password, provider-credential, connection, or database fields.
- **SC-009**: Across disabled configuration and Production-environment verification, zero development players are seeded and 100% of discovery/login attempts fail without exposing account choices or tokens.
- **SC-010**: When key protection is configured, 100% of missing, duplicated, or incorrect-key requests are rejected and 100% of valid-key requests proceed to normal operation validation.
- **SC-011**: A developer can discover an account, select it, obtain a real login result, and validate the returned identity through the current-user operation in under 10 seconds under normal development conditions.
- **SC-012**: Existing focused Firebase player-login scenarios continue to pass with no required consumer contract changes.
- **SC-013**: Automated and manual log inspection finds zero access tokens, development authorization keys, passwords, identity-provider tokens, database credentials, or connection strings emitted by this feature.

## Assumptions

- The current external player-login path remains the authoritative source for production player-token claims and normal `LoginResponse` meanings; development login converges on that existing behavior after selecting a seeded account/profile pair.
- The canonical seeded email or other stable lookup value may be derived deterministically from each account key using the reserved SprintLabs development namespace; it is not a user-entered login credential and is never returned as a secret.
- Initial seeded profiles may all use default progression values. Varied sample progression is optional and may be chosen during planning only if it remains deterministic and consistent with existing calculation rules.
- Development account keys are accepted only in their documented lowercase canonical form; clients discover and submit the returned value rather than relying on case or whitespace normalization.
- When development authentication is disabled or the environment is Production, returning the application's standard unavailable/not-found behavior is preferred over advertising that a disabled development feature exists.
- When a development authorization key is configured, invalid or missing key behavior follows the application's controlled unauthorized response conventions without distinguishing why validation failed.
- The optional authorization key protects development operations but is not intended to replace network-level access controls on shared development or staging deployments.
- This specification is backend-only. A later Unity feature will consume the safe list and normal login response without changing the backend contract defined here.
