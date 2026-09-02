# Research: Production-Like Development Player Authentication

## Decision 1: Reuse the Existing External-Player JWT Path

**Decision**: Development login converges on `ExternalPlayerLoginWorkflow` and `IUserService.Authenticate` after resolving an existing seeded User/Player pair.

**Evidence**: Firebase and Google handlers call `IExternalPlayerLoginWorkflow.CompleteAsync`. That workflow creates claims named `email`, `sub`, `name`, `userId`, and `playerProfileId`, calls `UserService.Authenticate`, and fills `LoginResponse` with the real account/profile snapshot. `UserService.Authenticate` signs with HMAC-SHA512 using `JWTOptions`; JWT bearer validation uses the same issuer, audience, key, lifetime validation, and clock skew. `/Users/me` reads the `userId` claim and reloads the linked Player.

**Alternatives considered**:

- `IAccessTokenService`: rejected because it is the separate teacher/community token path and omits `playerProfileId`.
- A development JWT generator: rejected because it would fork trusted identity and token validation.
- Reusing a Firebase/Google token: rejected because the feature replaces only that external-provider step.

## Decision 2: Add an Existing-Player-Only Workflow Entry

**Decision**: Add `CompleteExistingAsync` to the existing workflow and refactor claim creation/token issuance/response mapping into one private shared routine.

**Rationale**: Calling current `CompleteAsync` is unsafe because `ResolvePlayerAsync` can attach or create a Player and activate pending business records. HTTP development login must be read-only for accounts/profiles. A strict entry validates an existing User, an existing linked Player, account eligibility, and `Player.UserId == User.Id`, then uses the same token mapper.

**Alternatives considered**:

- Duplicate claims and LoginResponse mapping in the new handler: rejected because token behavior could drift.
- Add a new token facade/generic authentication framework: rejected as speculative abstraction.
- Call current workflow and check afterward: rejected because mutation may already have happened.

## Decision 3: Use One Shared Fail-Closed Guard

**Decision**: Define `IDevelopmentAuthenticationGuard` in Domain and implement it in Infrastructure using `IHostEnvironment` and `IOptions<DevelopmentAuthenticationOptions>`.

**Rationale**: Startup seeding and both HTTP handlers must apply one policy. Production is always denied first. Endpoints additionally require `Enabled`; seeding also requires `SeedPlayers`. If `ApiKey` is configured and non-whitespace, exactly one header value is required and compared in constant time. Disabled/Production returns concealed not-found behavior; bad configured keys return generic unauthorized behavior.

**Alternatives considered**:

- Controller-only checks: rejected because seeding and direct handler invocation could diverge.
- `IsDevelopment()` only: rejected because explicitly enabled staging is a valid non-Production use case.
- Plain string equality: rejected because a fixed-time comparison is inexpensive for a secret boundary.

## Decision 4: Use a Fixed Catalog, Not a Schema Change

**Decision**: A shared immutable catalog maps each exact account key to its Identity username, display name, and reserved email `dev-player-NN@development.sprintlabs.invalid`. `User.UserName` stores the account key; existing email and relationship fields locate the pair.

**Rationale**: There are exactly eight product-defined identities. Identity already has normalized username/email uniqueness, `Player.Email` and `Player.UserId` are indexed uniquely, and the database supplies real `long` IDs. A new `DevelopmentAccountKey` column would add a migration and production schema concept without improving this bounded feature.

**Alternatives considered**:

- New account-key column/index: rejected as unnecessary schema expansion.
- Generate IDs from key suffixes: rejected because database IDs remain authoritative.
- Store definitions separately in seeder and handlers: rejected because drift could authenticate the wrong pair.

## Decision 5: Seed Through Identity and the Existing DbContext

**Decision**: Implement `DevelopmentPlayerSeeder` with scoped `ApplicationDbContext` and `UserManager<User>`, following `DemoCommunitySeeder`'s relational transaction and idempotent ensure pattern.

**Rationale**: This preserves Identity normalization/validation and the real User-to-Player relationship. One serializable relational transaction handles the eight-player set. Exact username/email/UserId/ProfileId checks distinguish compatible seed state from ambiguous takeover. Startup calls it only after migration and only through the guard.

**State policy**:

- create passwordless, active, non-admin, non-teacher users without Google/Firebase IDs;
- create linked players with default consistent progression;
- preserve generated IDs and progression/totals accumulated after the first seed;
- reconcile catalog-owned username/email/name and safe missing relationships;
- fail on provider-owned, privileged, cross-linked, duplicate, or ambiguous rows;
- never lazily seed from an endpoint.

**Alternatives considered**:

- SQL/script seeding: rejected because it bypasses Identity and normal configuration.
- `HasData`: rejected because generated Identity records and idempotent relationship repair do not fit static model seeding.
- Create on login: rejected by the security and stable-identity requirements.

## Decision 6: Keep Discovery Minimal and Validate the Whole Set

**Decision**: Discovery returns only `accountKey` and `displayName`, ordered by key, after validating that all eight persisted pairs are usable.

**Rationale**: These are the only fields Unity Editor needs to choose an account. Returning profile IDs or emails expands disclosure, and including level/rank is optional. A partial list hides configuration corruption; a controlled conflict is more actionable and prevents presenting an unusable selector.

**Alternatives considered**:

- Return catalog entries without DB validation: rejected because the route promises available seeded accounts.
- Return partial valid entries: rejected because it masks broken seed state.
- Include Level/RankTier now: deferred because it is not required and would expand the selector contract.

## Decision 7: Strict Login Is Lookup-Only

**Decision**: The handler exact-matches the catalog, uses existing `IUserService.FindByEmail` and `IPlayerRepository.GetByUserIdAsync`, validates account status and relationship, and then calls `CompleteExistingAsync`.

**Rationale**: Existing services expose the authoritative user/profile IDs and persisted state. Missing, unknown, or mismatched state is an error. The `sub` claim uses the canonical account key as the mocked external subject, while trusted downstream identity remains the standard `userId` and `playerProfileId` claims.

**Eligibility note**: Existing external-player login rejects suspended Users. The development path must also reject a current Identity lockout and any teacher/platform-admin identity to satisfy the approved seed/account boundary. Add `UserName`, `IsTeacherAccount`, and `IsLockedOut` to the internal `UserIdentityResponse` projection and populate them in `UserService`; this does not change public login or `/Users/me` response contracts.

## Decision 8: Use Existing Response and Error Conventions

**Decision**: Both endpoints remain thin `AccountController` actions using MediatR and `BaseResponse<T>`. Controlled failures use `GenericException` and existing shared messages where possible.

| Condition | Status |
|---|---:|
| Success | 200 |
| Missing/malformed/non-canonical request value | 400 |
| Configured development key missing/duplicated/invalid | 401 |
| Seeded account suspended/locked/invalid | 403 |
| Feature disabled, Production, unknown/unseeded key | 404 |
| Catalog-backed User/Player state inconsistent | 409 |

Messages never contain the supplied key, configured key, token, database details, or collision data that could expose another identity.

## Decision 9: Prove the Real Bearer Path with a Narrow Test Host

**Decision**: Add one focused ASP.NET Core `TestServer` integration test using the real Account/Users routes, Identity-backed `UserService`, MediatR handlers, JWT bearer middleware, and an in-memory DbContext.

**Rationale**: The existing test project has no `WebApplicationFactory`, and full `Program` startup performs MySQL server auto-detection. A narrow host avoids production startup refactoring while still proving that the token returned by development-login passes real bearer validation and reaches `/Users/me`, whose response IDs match.

**Alternatives considered**:

- Decode the JWT only: rejected because it does not prove middleware/route authentication.
- Full `WebApplicationFactory<Program>`: rejected because it adds broader startup and MySQL substitution churn for one acceptance test.
- Optional live MySQL/Firebase test: rejected because deterministic automated validation must not require credentials or provider network access.

## Decision 10: Seed Default Progression Only

**Decision**: All eight new Players start with existing defaults: Gold/Experience/RP 0, Level 1, Student current/highest rank, zero matches/wins.

**Rationale**: Varied progression is optional. Defaults are internally consistent and require no progression service invocation. Re-runs preserve later changes so these accounts remain useful for gameplay testing.

## Decision 11: Add No Secret and Audit Only the Feature Diff

**Decision**: Commit false flags only; omit `ApiKey`; document User Secrets/environment/deployment secret injection; scan the final diff and logs.

**Rationale**: The repository already contains credential-like configuration unrelated to this feature. This work must neither echo nor expand those values. Existing secret remediation is reported separately rather than mixed into the feature.

## Confirmed Non-Goals

No Unity/Editor/Mirror code, profile API change, rank UI, match/question history, progression awarding, missions, telemetry, general auth rewrite, or generic seeding/DI framework is planned.
