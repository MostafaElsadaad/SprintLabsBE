# Implementation Plan: Production-Like Development Player Authentication

**Branch**: `codex/020-production-like-dev-auth` | **Date**: 2026-09-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/020-production-like-dev-auth/spec.md`

## Summary

Add an explicitly enabled, non-Production-only backend path that selects one of eight pre-seeded real `User` + `Player` pairs, then converges on the existing external-player token and `LoginResponse` path. The feature adds a safe discovery endpoint, a strict existing-player login endpoint, an idempotent startup seeder, one shared environment/configuration/API-key guard, focused tests, and consumer documentation.

The smallest design requires no database column or migration. A fixed catalog maps each canonical account key to a deterministic Identity username, reserved development email, and display name. The database continues to generate authoritative `long` identifiers. Development login must never call the current player-resolution branch that can create or attach a `Player`; instead, `ExternalPlayerLoginWorkflow` gains a narrow existing-player completion path that shares its claim construction, `IUserService.Authenticate` call, and `LoginResponse` mapping with Firebase/Google login.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core, ASP.NET Core Identity/UserManager, API versioning, MediatR 12, EF Core 8, Pomelo MySQL provider, JWT bearer authentication

**Storage**: Existing MySQL `Users` and `Players` through `ApplicationDbContext`; no new table, column, index, or migration

**Testing**: xUnit, FluentAssertions, Moq, EF Core InMemory, real `UserStore`/`UserManager`, and one focused ASP.NET Core `TestServer` bearer-authentication integration test

**Target Platform**: SprintLabs ASP.NET Core backend on local, shared development, or staging environments; unconditionally unavailable in Production

**Project Type**: Layered .NET web API with MediatR/CQRS vertical slices (`API`, `Application`, `Domain`, `Infrastructure`, `Shared`, `SprintLabs.Tests`)

**Performance Goals**: Discover eight accounts or complete one development login in well under one second under normal development conditions; the complete discover-select-login-`/Users/me` loop remains under the specification's 10-second outcome

**Constraints**: Disabled by default; Production always wins over configuration; no Firebase/Google call; no fake token or ID; no lazy account/profile creation from HTTP; no new JWT implementation; no Unity/Mirror change; no progression award or recalculation; no committed secret; no unrelated authentication refactor

**Scale/Scope**: Exactly eight deterministic accounts, two versioned Account endpoints, one startup seeder, one shared safety guard, one strict extension to the existing external-player login workflow, and focused backend tests/docs

**Clarifications**: None. Phase 0 research resolved token reuse, deterministic lookup, persistence, startup gating, API-key handling, error behavior, and test-host strategy.

## Constitution Check

*GATE: Passed before research and re-checked after design.*

| Principle | Design compliance |
|---|---|
| Vertical slices | Delivers configuration, seeding, strict login logic, two API contracts, tests, and feature documentation as one backend slice. |
| Existing architecture wins | Reuses `AccountController`, MediatR, `ExternalPlayerLoginWorkflow`, `IUserService.Authenticate`, `IPlayerRepository`, `ApplicationDbContext`, `UserManager<User>`, `BaseResponse`, and `GenericException`. |
| SaaS data isolation | Not a tenant-data feature. Seeded users receive no community, staff, teacher, or platform-admin privilege; collisions with privileged/provider-owned identities fail safely. |
| JSON where flexibility matters | Not applicable. |
| Minimum useful implementation | Uses a fixed catalog and existing unique identity fields, so no schema migration, new token service, provider adapter, generic seeding framework, or progression setup is introduced. |
| Quality gates | Adds focused unit/persistence/API tests, one real bearer middleware test through `/Users/me`, targeted Firebase regression coverage, solution build/test, and a diff secret scan. |
| Documentation is executable context | Plan, research, model, contract, API guide, backend-only consumer note, and quickstart live under feature 020. |

**Post-design re-check**: Passed. The guard interface is justified by one security policy shared across startup and request handlers. Extending the existing login workflow with an existing-player-only entry point removes real token/response duplication and prevents HTTP-time provisioning. No complexity exception is required.

## Project Structure

### Documentation (this feature)

```text
specs/020-production-like-dev-auth/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── api.md
├── frontend.md
├── contracts/
│   └── development-authentication-api.md
└── tasks.md                              # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
API/
├── Controllers/AccountController.cs
├── Program.cs
└── appsettings.json

Application/Features/Accounts/
├── Common/
│   ├── IExternalPlayerLoginWorkflow.cs
│   └── ExternalPlayerLoginWorkflow.cs
└── DevelopmentAuthentication/
    ├── DevelopmentLogin/
    │   ├── DevelopmentLoginRequest.cs
    │   ├── DevelopmentLoginCommand.cs
    │   └── DevelopmentLoginCommandHandler.cs
    └── ListDevelopmentPlayers/
        ├── ListDevelopmentPlayersQuery.cs
        ├── ListDevelopmentPlayersQueryHandler.cs
        └── DevelopmentPlayerResponse.cs

Domain/Services/
└── IDevelopmentAuthenticationGuard.cs

Infrastructure/
├── Seed/DevelopmentPlayerSeeder.cs
├── Services/DevelopmentAuthenticationGuard.cs
└── ServiceConfig.cs

Shared/
├── DevelopmentAuthentication/DevelopmentPlayerCatalog.cs
├── Options/DevelopmentAuthenticationOptions.cs
└── Responses/UserIdentityResponse.cs       # Adds UserName/teacher/lockout eligibility metadata

SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/
├── DevelopmentAuthenticationGuardTests.cs
├── DevelopmentPlayerSeederTests.cs
├── DevelopmentLoginCommandHandlerTests.cs
├── ListDevelopmentPlayersQueryHandlerTests.cs
├── DevelopmentAuthenticationControllerTests.cs
└── DevelopmentAuthenticationEndToEndTests.cs
```

**Structure Decision**: Extend the current layered solution and Account CQRS feature. The immutable catalog belongs in `Shared` because both Application handlers and the Infrastructure seeder require the exact same eight definitions. The guard contract belongs in `Domain/Services` and its environment/options-aware implementation in `Infrastructure`. No project, production database model, or migration is added.

## Existing Token-Issuance Path to Reuse

```text
POST Account/firebase-login or google-login
  -> provider-specific MediatR handler
  -> IUserService.FindOrCreate...User
  -> IExternalPlayerLoginWorkflow.CompleteAsync
       -> resolve/link/create Player
       -> optional membership activation
       -> claims: email, sub, name, userId, playerProfileId
       -> IUserService.Authenticate
            -> UserService.Authenticate
            -> JWTOptions issuer/audience/secret
            -> HMAC-SHA512, existing seven-day player-token lifetime
       -> populate existing LoginResponse from User + Player
```

`IAccessTokenService` is not reused: it is the separate teacher/community token path and does not issue the current player claim set. Development login will use a strict `CompleteExistingAsync` path in `ExternalPlayerLoginWorkflow`; both workflow entries delegate to one private issuer/mapper. The strict path validates the supplied persisted relationship and account state, performs no provider verification, provisioning, linking, activation, progression mutation, or profile repair, and then calls the same `IUserService.Authenticate` method.

## Configuration and Security Design

Committed configuration contains only disabled non-secret defaults:

```json
{
  "DevelopmentAuthentication": {
    "Enabled": false,
    "SeedPlayers": false
  }
}
```

`ApiKey` is omitted from committed settings. Local callers use User Secrets or environment variables such as `DevelopmentAuthentication__ApiKey`; deployed non-Production systems use deployment secret configuration. `IDevelopmentAuthenticationGuard` applies this order on every endpoint and at startup:

1. deny unconditionally when `IHostEnvironment.IsProduction()`;
2. require `Enabled` for endpoints;
3. additionally require `SeedPlayers` before startup seeding;
4. if a non-whitespace `ApiKey` is configured, require exactly one `X-SprintLabs-Dev-Key` value and compare it in constant time;
5. never return or log the configured/provided key.

Disabled or Production endpoints use a controlled 404 to conceal the development surface. Missing, duplicate, or invalid configured keys return a generic 401. The guard runs before catalog/account lookup. Canonical keys use ordinal, case-sensitive matching with no trimming.

## Persistence and Seeding Design

`DevelopmentPlayerCatalog` owns exactly eight immutable definitions: `dev-player-01` through `dev-player-08`, matching `Dev Player 01` through `Dev Player 08`, and emails `dev-player-01@development.sprintlabs.invalid` through `dev-player-08@development.sprintlabs.invalid`. The account key is persisted as `User.UserName`; email/normalized email provides the existing `IUserService.FindByEmail` lookup. Existing Identity username/email uniqueness plus unique `Player.UserId`/email indexes are the integrity backstops.

`DevelopmentPlayerSeeder` uses the scoped `ApplicationDbContext` and `UserManager<User>` pattern already used by `DemoCommunitySeeder`. In relational storage it operates in one serializable transaction for the eight-account set. It creates passwordless, active, non-admin, non-teacher users with no Google/Firebase identifiers and linked players with default consistent progression (`Experience=0`, `Level=1`, `Rp=0`, Student rank). Re-runs preserve generated IDs and progression earned after seeding; they reconcile only catalog-owned username/email/display/active relationship values. Compatible missing profiles may be recreated only by the seeder. Provider-linked, privileged, cross-linked, duplicate, or otherwise ambiguous collisions abort safely instead of being taken over.

HTTP discovery/login never invokes the seeder and never repairs state. Discovery validates the entire catalog and returns all eight safe items or a controlled conflict; it never returns a partial usable list.

## API and CQRS Design

### Development player discovery

- `GET /api/v1/Account/development-players`
- `[AllowAnonymous]` at bearer level, but protected by the shared non-Production/configuration/API-key guard.
- Controller passes raw header values to `ListDevelopmentPlayersQuery`, preserving duplicate detection.
- Handler validates each catalog entry with existing `IUserService.FindByEmail` and `IPlayerRepository.GetByUserIdAsync` and verifies the authoritative link/state.
- Response is `BaseResponse<List<DevelopmentPlayerResponse>>`; each item contains only `accountKey` and `displayName`, in canonical order. Level/rank are deliberately deferred because the minimum selector does not need them.

### Development login

- `POST /api/v1/Account/development-login`
- Request: `{ "accountKey": "dev-player-03" }`, plus optional configured header.
- Controller passes raw header values and the body value into `DevelopmentLoginCommand`.
- Handler runs the guard first, exact-matches the catalog, resolves existing User and Player, validates active/not-suspended/not-currently-locked status and the exact relationship, then calls `IExternalPlayerLoginWorkflow.CompleteExistingAsync`.
- Response is the existing `BaseResponse<LoginResponse>` with unchanged fields and real database identities.
- The JWT `sub` uses the deterministic account key as the mocked external subject; all trusted downstream identity continues to come from the unchanged `userId` and `playerProfileId` claims.

Controlled errors are: 400 for missing/malformed/non-canonical input, 401 for configured-key failure, 403 for invalid account status, 404 for disabled/Production/unknown/unseeded accounts, and 409 for inconsistent seeded identity/profile state. No failure creates or modifies data.

## Implementation Sequence

Each step below is implementation work for the later task phase; this planning command does not modify application code.

| # | Files/classes changed | Existing code reused | New code required | Security boundary | Database behavior | Validation |
|---:|---|---|---|---|---|---|
| 1 | Document `ExternalPlayerLoginWorkflow`, `UserService.Authenticate`, `Infrastructure/ServiceConfig`, `UsersController` in feature docs | Exact Firebase/Google player token flow, JWT bearer validator, `/Users/me` | No production code | Establishes `userId` + `playerProfileId` claims as the only trusted downstream identity | Read-only inspection | Existing Firebase workflow tests and JWT claim inspection establish baseline |
| 2 | `Shared/Options/DevelopmentAuthenticationOptions.cs`, `API/appsettings.json` | Existing options binding style in `Infrastructure/ServiceConfig` | `Enabled`, `SeedPlayers`, nullable `ApiKey`, section/header constants | Defaults false; no committed secret value | None | Options default/key-whitespace tests and config inspection |
| 3 | `Domain/Services/IDevelopmentAuthenticationGuard.cs`, `Infrastructure/Services/DevelopmentAuthenticationGuard.cs`, `Infrastructure/ServiceConfig.cs` | `IHostEnvironment`, options, `GenericException` conventions | Shared endpoint/seed gate and fixed-time optional-key comparison | Production denial is unconditional; guard precedes account lookup | None | Matrix tests for Production, disabled, enabled, seed flag, absent/valid/invalid/duplicate key |
| 4 | `Shared/DevelopmentAuthentication/DevelopmentPlayerCatalog.cs`, `Infrastructure/Seed/DevelopmentPlayerSeeder.cs` | `DemoCommunitySeeder`, `ApplicationDbContext`, `UserManager<User>` | Fixed definitions and idempotent ensure/reconcile algorithm | Reject provider-linked/privileged/cross-owned collisions; never log secrets | One serializable relational transaction; no schema change | First-run, partial-state, collision, and rollback tests |
| 5 | `DevelopmentPlayerCatalog`, `DevelopmentPlayerSeeder` | Existing User/Player defaults and one-to-one relationship | Eight passwordless User + Player pairs | Users are active, non-admin, non-teacher, and have no external provider IDs | Database-generated `long` IDs; unique username/email/link constraints | Assert exact count, names, distinct IDs, and links |
| 6 | Catalog and seeder lookup code; `Shared/Responses/UserIdentityResponse.cs`; `Infrastructure/Services/UserService.cs` | Identity normalized username/email and existing `FindByEmail` | Stable key→username/email mapping with ordinal lookup; expose UserName, teacher-account flag, and current-lockout eligibility in the internal identity projection | Canonical lowercase key only; no fuzzy matching/takeover or privileged/locked account | Reuses current unique indexes; no account-key column | Case, whitespace, unknown key, lockout/privilege, and incompatible collision tests |
| 7 | Seeder profile initialization | Existing Player defaults; progression entities/services remain untouched | Default-only seed profile values | No reward/progression privilege | New profiles start XP/RP/gold 0, level 1, Student; reruns do not reset accrued values | Assert internal consistency and rerun preservation |
| 8 | `AccountController`, `Application/.../ListDevelopmentPlayers/*` | Route versioning, MediatR, `BaseResponse`, `IUserService`, `IPlayerRepository` | GET query/handler and safe response DTO | Shared guard/key executes first; no IDs/secrets/tokens in response | Read-only; fails whole request on invalid seed state | Controller contract, ordering, safe-field, unavailable/key/state tests |
| 9 | `AccountController`, `Application/.../DevelopmentLogin/*` | MediatR, `FindByEmail`, Player repository | POST request/command/handler with strict catalog/account resolution | Guard before lookup; exact account state/link validation | Read-only login; no lazy create/link/repair | Success, repeat, distinct, unknown, malformed, suspended/locked, missing/mismatched profile tests |
| 10 | `IExternalPlayerLoginWorkflow`, `ExternalPlayerLoginWorkflow` | Existing claim names, `IUserService.Authenticate`, `LoginResponse` mapper | Strict existing-player entry and one shared private issue/map routine | Validates `Player.UserId == User.Id`; no provider/member activation | Reads User/Player only; token issuance is non-persistent | Existing Firebase/Google workflow tests plus strict-path claim/response tests |
| 11 | Guard and controller/command header plumbing | Existing exception middleware and response envelope | Exactly-one header handling and fixed-time comparison | Key never enters claims, response, or logs | None | Missing, duplicate, malformed, invalid, valid, and no-key-configured cases |
| 12 | `API/Program.cs` | Current scope/migrate/seeder startup pattern | Resolve guard and invoke seeder only when `CanSeed`; safe count/key/ID logging | Production/disabled paths never invoke seeder | Seeder transaction occurs after migrations; collision fails startup visibly | Program/startup policy test plus direct seeder tests |
| 13 | `SprintLabs.Tests/Features/DevelopmentPlayerAuthentication/*`, test project package reference if needed | xUnit/Moq/InMemory/UserManager conventions and real JWT bearer configuration | Focused unit/persistence/controller tests and narrow `TestServer` flow | Real bearer middleware protects real `/Users/me` route | InMemory store for automated tests; no external DB/Google/Firebase | All required scenarios, including dev-login token → `/Users/me` identity equality |
| 14 | Existing Firebase auth tests; no Firebase code contract change | `FirebaseAuthenticationControllerTests`, handler/workflow tests | Only test adjustments required by shared private workflow extraction | Confirms no bypass or provider-policy regression | Existing Firebase persistence semantics unchanged | Run focused Firebase + external workflow suite |
| 15 | Git diff and feature docs | Existing `.gitignore`, User Secrets support, documentation rules | Secret-pattern review and manual configuration guidance | Reject any new API key, JWT, DB credential, token, or provider secret in diff/logs | No data action | `git diff --check`, targeted secret searches, build/test, manual log inspection |

## Testing Strategy

The test suite adds `Microsoft.AspNetCore.TestHost` only if required for a narrow in-memory host; it does not expose `Program` or refactor production startup. The end-to-end test hosts the real Account and Users controllers, MediatR handlers, Identity-backed `UserService`, JWT bearer validation, and in-memory `ApplicationDbContext`; it seeds a development account, calls development-login, sends the returned bearer token to the actual `/api/v1/Users/me` route, and asserts both `long` identifiers match.

Focused groups:

- catalog and guard: exact keys, safe defaults, Production override, seed flag, optional key semantics, duplicate header rejection;
- seeder: first/second/third run, stable/distinct IDs, complete eight-player set, partial compatible repair, conflict refusal, progression preservation;
- discovery: exact order/safe shape, no partial invalid list, all safety failures;
- login: normal `LoginResponse`, real token, repeated/different identity, unknown/no mutation, invalid User/Player state;
- compatibility: existing Firebase controller, handler, workflow, and persistence tests;
- quality: `dotnet build SprintLabs.sln`, targeted tests, then `dotnet test SprintLabs.sln` when the environment permits.

## Documentation Plan

- `contracts/development-authentication-api.md`: exact HTTP contracts, headers, envelopes, and errors.
- `api.md`: backend/consumer reference and security/configuration notes.
- `frontend.md`: explicitly records that no frontend/Unity implementation belongs to this feature; provides only future consumer states required by the repository documentation rule.
- `quickstart.md`: local User Secrets/environment setup, seed/login/`/Users/me` verification, Production-negative test, automated commands, and secret scan.
- Do not add Unity, Mirror, match, profile UI, or progression implementation steps.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Current `CompleteAsync` provisions/links Players | Never call it from development login; add strict existing-player completion and share only issue/map logic. |
| Development surface enabled on an internet-accessible staging host | Require explicit non-Production enablement and support an independently configured header key; network controls remain recommended. |
| Production misconfiguration | `IsProduction()` denial is evaluated first for endpoints and seeding. |
| Catalog identity collides with a real/provider/privileged account | Reserved deterministic namespace, exact multi-key checks, and fail-safe seeder behavior; never strip privileges or provider IDs. |
| Re-running seed resets useful game state | Preserve IDs and all progression/totals; reconcile only catalog-owned identity/display/link fields. |
| Concurrent startup races | Serializable transaction plus existing unique constraints; one process may fail safely, but duplicates cannot be accepted. |
| JWT test validates only token syntax | Use real JWT bearer middleware and the actual `/Users/me` route in the focused TestServer test. |
| Existing repository configuration already contains credential-like values | Do not alter or repeat unrelated existing values in this feature; report them separately and ensure the feature diff introduces no new secret. |
| Global exception middleware logs exception text | Development failures use generic messages that never contain provided/configured keys, tokens, or connection details. |

## Complexity Tracking

No constitution violations require justification.
