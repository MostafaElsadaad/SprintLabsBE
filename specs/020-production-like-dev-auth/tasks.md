# Tasks: Production-Like Development Player Authentication

**Input**: Design documents from `specs/020-production-like-dev-auth/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [HTTP contract](contracts/development-authentication-api.md)

**Tests**: Required by the approved specification. Write focused tests before the corresponding implementation and confirm they fail for the intended reason.

**Organization**: Tasks are grouped by user story. Security Story 3 is scheduled first because its guard blocks seeding and both endpoints.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes different files and has no dependency on another incomplete task.
- **[Story]**: Maps the task to a user story in `spec.md`.
- Every task includes concrete repository paths.

## Phase 1: Setup and Authentication Baseline

**Purpose**: Confirm the production player-token path that all development login work must reuse.

- [X] T001 Inspect and regression-run the existing player JWT path in `API/Controllers/AccountController.cs`, `Application/Features/Accounts/{FirebaseAuthenticate,GoogleAuthenticate,Common}/`, `Infrastructure/Services/UserService.cs`, `Infrastructure/ServiceConfig.cs`, `Shared/Responses/LoginResponse.cs`, and `API/Controllers/UsersController.cs`; record any discovered mismatch in `specs/020-production-like-dev-auth/research.md` before implementation

---

## Phase 2: Foundational Configuration and Catalog

**Purpose**: Add the disabled-by-default configuration and single deterministic account definition used by seeding and both APIs.

**Critical**: Complete this phase before any user-story implementation.

- [X] T002 [P] Add `DevelopmentAuthenticationOptions` with `Enabled`, `SeedPlayers`, optional `ApiKey`, and the fixed header name in `Shared/Options/DevelopmentAuthenticationOptions.cs`; bind it in `Infrastructure/ServiceConfig.cs` and add only disabled non-secret defaults to `API/appsettings.json`
- [X] T003 [P] Add the immutable eight-entry key/name/email mapping with ordinal exact lookup in `Shared/DevelopmentAuthentication/DevelopmentPlayerCatalog.cs`, using `dev-player-01` through `dev-player-08` and the reserved `development.sprintlabs.invalid` email namespace

**Checkpoint**: Configuration defaults are safe and one catalog defines every stable development identity.

---

## Phase 3: User Story 3 - Prevent Unsafe Development Authentication (Priority: P1)

**Goal**: Make Production an unconditional denial and enforce explicit enablement plus optional development-key authorization everywhere.

**Independent Test**: Exercise the guard with Production, disabled, enabled, seed-disabled, no-key, valid-key, invalid-key, missing-key, and duplicate-header inputs; only explicitly enabled, authorized, non-Production requests may proceed, and seeding additionally requires `SeedPlayers`.

### Tests for User Story 3

- [X] T004 [US3] Add failing guard-policy tests for the full environment/configuration/header matrix, including constant-time key behavior outcomes and Production precedence, in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentAuthenticationGuardTests.cs`

### Implementation for User Story 3

- [X] T005 [US3] Implement and register the shared fail-closed policy in `Domain/Services/IDevelopmentAuthenticationGuard.cs`, `Infrastructure/Services/DevelopmentAuthenticationGuard.cs`, and `Infrastructure/ServiceConfig.cs`; return concealed 404 behavior for disabled/Production, generic 401 for configured-key failures, reject duplicate headers, and never log or return either key value

**Checkpoint**: The safety boundary is independently testable and ready for startup, login, and discovery consumers.

---

## Phase 4: User Story 1 - Ensure Deterministic Development Players (Priority: P1) 🎯 MVP

**Goal**: Explicitly enabled non-Production startup ensures eight stable real User + Player pairs without duplicates or progression resets.

**Independent Test**: Run the seeder three times over an empty isolated store and verify exactly eight distinct stable `long` UserIds and PlayerProfileIds; verify compatible partial repair, collision refusal, and preservation of changed progression.

### Tests for User Story 1

- [X] T006 [US1] Add failing first-run, repeat-run, stable/distinct identity, partial-state, incompatible-collision, and progression-preservation tests using real `UserManager<User>` plus EF InMemory in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentPlayerSeederTests.cs`

### Implementation for User Story 1

- [X] T007 [US1] Implement the idempotent `ApplicationDbContext`/`UserManager<User>` seeder with one serializable relational transaction, passwordless active non-privileged Users, linked default-progression Players, safe reconciliation, and collision refusal in `Infrastructure/Seed/DevelopmentPlayerSeeder.cs`
- [X] T008 [US1] Invoke `DevelopmentPlayerSeeder` after migration only when the shared guard allows seeding, and add safe count/key/UserId/PlayerProfileId logging without secrets in `API/Program.cs`

**Checkpoint**: Eight real development identities can be ensured repeatedly while Production and disabled configurations invoke no seeding.

---

## Phase 5: User Story 2 - Log In with a Real SprintLabs Token (Priority: P1)

**Goal**: A canonical seeded key returns the existing `LoginResponse` and a normal player JWT accepted by unchanged `/Users/me` authentication.

**Independent Test**: Log in repeatedly as `dev-player-01`, confirm stable real IDs and no row creation, log in as `dev-player-02` and confirm different IDs, then send the returned bearer token to `/api/v1/Users/me` and confirm both IDs match.

### Tests for User Story 2

- [X] T009 [P] [US2] Add failing tests for strict existing-player completion, unchanged claims, `LoginResponse` mapping, relationship rejection, and absence of provisioning/activation in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/ExternalPlayerLoginWorkflowTests.cs`
- [X] T010 [P] [US2] Add failing handler tests for success, canonical-key enforcement, unknown/unseeded key, suspended/locked/privileged User, missing/mismatched Player, repeated stable identity, different identities, configured-key failure, Production, disabled configuration, and zero mutation on rejection in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentLoginCommandHandlerTests.cs`
- [X] T011 [P] [US2] Add failing route/envelope/request/header contract tests for `POST /api/v1/Account/development-login` in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentAuthenticationControllerTests.cs`

### Implementation for User Story 2

- [X] T012 [US2] Add `UserName`, `IsTeacherAccount`, and current-lockout metadata to `Shared/Responses/UserIdentityResponse.cs`, populate it without changing public response contracts in `Infrastructure/Services/UserService.cs`, and add a strict existing-player completion method that shares the existing claims, `IUserService.Authenticate`, and `LoginResponse` mapper in `Application/Features/Accounts/Common/{IExternalPlayerLoginWorkflow.cs,ExternalPlayerLoginWorkflow.cs}`
- [X] T013 [US2] Implement canonical lookup-only development login with no Google/Firebase call and no create/link/repair behavior in `Application/Features/Accounts/DevelopmentAuthentication/DevelopmentLogin/{DevelopmentLoginRequest.cs,DevelopmentLoginCommand.cs,DevelopmentLoginCommandHandler.cs}`
- [X] T014 [US2] Add the thin `[AllowAnonymous]` `POST development-login` action using MediatR, raw header values, existing `BaseResponse<LoginResponse>`, and documented error responses in `API/Controllers/AccountController.cs`
- [X] T015 [US2] Add `Microsoft.AspNetCore.TestHost` only if needed in `SprintLabs.Tests/Compass.Tests.csproj`, then implement an isolated real JWT-bearer flow from development-login through the actual `/api/v1/Users/me` route, including repeated-login and cross-player ID assertions, in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentAuthenticationEndToEndTests.cs`

**Checkpoint**: Development login differs only before the existing SprintLabs JWT/LoginResponse boundary; downstream bearer identity is production-like.

---

## Phase 6: User Story 4 - Discover Safe Development Account Choices (Priority: P2)

**Goal**: Return exactly the eight usable account keys/display names without secrets, tokens, emails, or database identities.

**Independent Test**: With eight valid seeded pairs, GET discovery returns eight items in key order and only safe fields; invalid pair state, Production, disabled configuration, and invalid configured keys return no account list.

### Tests for User Story 4

- [X] T016 [P] [US4] Add failing query tests for exact order, safe projection, all-or-nothing invalid-state handling, no mutation, Production/disabled denial, and optional-key enforcement in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/ListDevelopmentPlayersQueryHandlerTests.cs`
- [X] T017 [P] [US4] Add failing route/envelope/header and response-shape tests for `GET /api/v1/Account/development-players` in `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/DevelopmentAuthenticationControllerTests.cs`

### Implementation for User Story 4

- [X] T018 [US4] Implement lookup-only validation and the minimal `accountKey`/`displayName` response in `Application/Features/Accounts/DevelopmentAuthentication/ListDevelopmentPlayers/{ListDevelopmentPlayersQuery.cs,ListDevelopmentPlayersQueryHandler.cs,DevelopmentPlayerResponse.cs}`
- [X] T019 [US4] Add the thin `[AllowAnonymous]` `GET development-players` action using MediatR, raw header values, `BaseResponse<List<DevelopmentPlayerResponse>>`, and documented error responses in `API/Controllers/AccountController.cs`

**Checkpoint**: A future Editor consumer can discover usable accounts without receiving credentials or authoritative database IDs.

---

## Phase 7: User Story 5 - Preserve Production Authentication Compatibility (Priority: P2)

**Goal**: Firebase/Google player login and unchanged downstream bearer authentication continue to behave as before.

**Independent Test**: Existing Firebase controller, handler, persistence, activation, and external workflow tests pass unchanged in behavior; the development token uses the same claim validator and `/Users/me` path.

### Tests and Verification for User Story 5

- [X] T020 [US5] Update only assertions/fixtures required by the shared workflow extraction, while preserving Firebase/Google behavior, in `SprintLabs.Tests/Features/FirebasePlayerAuthentication/*.cs`, `SprintLabs.Tests/Features/B2CPlayerProfileSupport/B2CLoginCompatibilityTests.cs`, and `SprintLabs.Tests/Features/StudentLicenseActivation/StudentLicenseActivationOnLoginTests.cs`
- [X] T021 [US5] Run the focused Firebase/external-player regression suite from `SprintLabs.Tests/Compass.Tests.csproj` and resolve only feature-caused failures in `Application/Features/Accounts/Common/ExternalPlayerLoginWorkflow.cs` and the development-authentication slice

**Checkpoint**: Existing production player authentication contracts and business activation behavior remain unchanged.

---

## Phase 8: Polish and Cross-Cutting Validation

**Purpose**: Validate the complete feature, synchronize documentation, and prove no secret was introduced.

- [X] T022 [P] Update implemented route/configuration/error/manual-validation details if they differ from design in `specs/020-production-like-dev-auth/{api.md,frontend.md,quickstart.md,contracts/development-authentication-api.md}` without adding Unity or Mirror implementation scope
- [X] T023 Run `dotnet build SprintLabs.sln`, the focused DevelopmentPlayerAuthentication/FirebasePlayerAuthentication tests, `dotnet test SprintLabs.sln`, and the Production/disabled/stable-identity manual checks from `specs/020-production-like-dev-auth/quickstart.md`; fix only feature-scoped failures
- [X] T024 Run `git diff --check` and scan the feature diff/source/log assertions for development API keys, JWTs, passwords, connection strings, provider tokens, or other credentials; remove any newly introduced secret from `API/`, `Application/`, `Domain/`, `Infrastructure/`, `Shared/`, `SprintLabs.Tests/`, and `specs/020-production-like-dev-auth/`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Starts immediately.
- **Foundational (Phase 2)**: Depends on T001 and blocks all user stories.
- **US3 Safety (Phase 3)**: Depends on T002 and blocks seeder and endpoint wiring.
- **US1 Seeding (Phase 4)**: Depends on T003 and T005.
- **US2 Login (Phase 5)**: Depends on T003, T005, and a valid seeded pair contract from US1; implementation can use test fixtures before startup wiring is complete.
- **US4 Discovery (Phase 6)**: Depends on T003, T005, and the US1 seeded-pair invariants.
- **US5 Compatibility (Phase 7)**: Depends on US2 shared-workflow changes.
- **Polish (Phase 8)**: Depends on all selected stories.

### User Story Dependencies

```text
Foundation
   |
   v
US3 safety guard
   |
   +----> US1 deterministic seeding ----> US2 real-token login ----> US5 regression
   |                    |
   |                    `-----------> US4 safe discovery
   `--------------------------------> all request/startup denial checks
```

- **US3** is a security prerequisite and can be tested independently as a policy.
- **US1** is the smallest useful data slice and does not require either endpoint.
- **US2** and **US4** both consume the same catalog/guard/seed invariants and can proceed in parallel after US1 foundations are stable.
- **US5** validates that US2's shared workflow extraction did not alter production login.

### Within Each Story

- Add focused tests first and confirm they fail for the missing behavior.
- Implement shared/internal behavior before controller actions.
- Keep controllers thin and route all behavior through MediatR.
- Validate the story checkpoint before starting dependent work.

## Parallel Opportunities

- T002 and T003 can run in parallel.
- T009, T010, and T011 can be written in parallel before US2 implementation.
- T016 and T017 can be written in parallel before US4 implementation.
- After US1 invariants and the guard are stable, US2 login and US4 discovery can be implemented in parallel, coordinating only changes to `AccountController.cs`.
- T022 documentation synchronization can run alongside final automated validation after contracts stabilize.

## Parallel Example: User Story 2

```text
Task T009: Add strict shared-workflow tests in ExternalPlayerLoginWorkflowTests.cs
Task T010: Add development-login handler tests in DevelopmentLoginCommandHandlerTests.cs
Task T011: Add POST controller contract tests in DevelopmentAuthenticationControllerTests.cs
```

## Parallel Example: User Story 4

```text
Task T016: Add discovery query tests in ListDevelopmentPlayersQueryHandlerTests.cs
Task T017: Add GET controller contract tests in DevelopmentAuthenticationControllerTests.cs
```

## Implementation Strategy

### MVP First

1. Complete T001-T005: baseline, configuration, catalog, and hard safety guard.
2. Complete T006-T008: deterministic idempotent seeding.
3. Stop and validate US1 independently: eight stable real User + Player pairs, no duplicates, no Production/disabled seeding.

### Incremental Delivery

1. Add US2 login and prove its real JWT through `/Users/me`.
2. Add US4 safe account discovery.
3. Complete US5 Firebase/Google compatibility verification.
4. Run complete build/test/manual/security validation.

## Notes

- No task introduces Unity, Mirror, profile expansion, match/question history, progression processing, missions, telemetry, or unrelated cleanup.
- No migration or parallel authentication/token service is planned.
- `IUserService.Authenticate` and existing `LoginResponse` remain authoritative.
- Commit after each task or small logical group and preserve unrelated worktree changes.
