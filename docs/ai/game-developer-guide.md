# Game developer backend guide

Read AGENTS.md first. This guide covers content seeds and small API changes alongside Unity work. Use your own checkout paths and private credentials. Documentation does not grant repository, admin or deployment access.

## Prepare both repositories

In each repo, inspect before editing:

```powershell
git status --short
git branch --show-current
git log -1 --oneline
git remote -v
git fetch origin
```

Preserve unrelated changes. Start a feature branch in each repository that needs edits, normally based on updated backend `origin/development` and Unity `origin/main`; verify the intended bases. Use `codex/<task>` unless another name is specified. Use a separate worktree when needed instead of resetting someone else's work.

Read backend `docs/ai/current-state.md` and the relevant feature contracts. In Unity, read AGENTS.md, PROJECT_HANDOFF.md and applicable nested instructions. Inspect actual adapters/screens before assuming integration exists.

## Run the backend locally

Prerequisites: .NET 8 SDK and a reachable MySQL development database provisioned for you. Follow the existing provider/version requirements. SQLite supports some tests, not the normal runtime setup.

Configure the API through .NET User Secrets or process environment variables. Do not commit secrets to appsettings, Unity assets or docs.

| Setting | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | Your development MySQL database |
| `JWTOptions:Secret`, `JWTOptions:Issuer`, `JWTOptions:Audience` | Local token signing/validation; use a strong private signing key |
| `Authentication:Firebase:ProjectId` | Intended Firebase project when using that login flow |
| `GOOGLE_APPLICATION_CREDENTIALS` | Private service-account file path where required for Firebase login |
| `DevelopmentAuthentication:Enabled`, `DevelopmentAuthentication:SeedPlayers` | Optional development login and eight repeat-safe player seeds |
| `DevelopmentAuthentication:ApiKey` | Private development-login guard key for shared hosts |
| `GameServer:ApiKey` | Private trusted-server credential for match writes |

Environment equivalents replace `:` with `__`, for example `ConnectionStrings__DefaultConnection`. Obtain approved values through the team's private channel. Configure the integrations your task needs; inspect startup errors/source for further required settings rather than inventing values.

Launch profiles contain a workstation-specific Firebase path. Bypass them below and supply your own private configuration. Run from the backend root in PowerShell:

```powershell
dotnet restore SprintLabs.sln
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5153'
dotnet run --project API/API.csproj --no-launch-profile
```

Swagger: `http://localhost:5153/swagger`. Configuration detects MySQL server version, requiring a reachable database. Startup attempts pending migrations and logs failures; a running HTTP process does not prove migration success. **Verify the database target before starting**, since startup can modify it. Check startup output and schema.

For deliberate migration work, with a repository-compatible `dotnet-ef` tool available:

```powershell
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

Run updates only against the intended authorized database. Schema changes require migrations/model snapshot updates; content-only changes usually do not.

## Seed and edit content

Use Swagger or an HTTP client with the target environment's **platform-admin JWT**. Player JWTs, development login keys and GameServer keys are different credentials. Never put admin or GameServer credentials in the shipped client.

| Task | Existing mechanism | Notes |
|---|---|---|
| Add/edit shop character | `PUT /api/v1/admin/shop/products/{itemKey}` | Stable-key upsert; map iconKey/prefabKey to bundled Unity assets; set isActive explicitly |
| Seed example shop listing | `POST /api/v1/admin/shop/seed/example` | Explicit, repeat-safe King of Knowledge at 500 coins; example asset keys need game mapping |
| Weekly/seasonal game missions | `POST /api/v1/admin/missions/seed/game` | Repeat-safe current/next week and calendar-quarter activations; inspect contract for rewards/dates |
| Publish relational question | `PUT /api/v1/Question/bank` | Preserve shared UUID and typed answer IDs; complete request in question contract |
| Import legacy pack | `POST /api/v1/Question/bank/import-legacy/{packId}` | Validates/copies, retains source; repeat-safe for unchanged content |
| Education option examples | `AddB2CPlayerSetup` migration | Seeds Egyptian National/Arabic, Egyptian National/English and IG/English; qualifying Questions pairs also appear |
| Development players | Startup with development authentication flags | Eight known accounts, preserving progression on reseed; disabled in Production |
| Local platform admin | `PlatformAdminSeed:Username`, `Email`, `Password`, optional `Name` | Startup creates an admin when required values are provided; does not promote existing non-admins |

There is no dedicated education-options admin API in this feature. If one becomes necessary, add a small authorized slice; do not invent an endpoint or insert fake questions to populate options.

Complete contracts, bodies and errors:

- [Questions](../../specs/026-question-data-scheme/api.md)
- [Missions](../../specs/023-missions-system/api.md): `/seed` is the older demo; use `/seed/game` for game content.
- [Shop](../../specs/030-coin-shop/api.md)
- [B2C setup](../../specs/029-b2c-player-setup/api.md)
- [Development login](../../specs/020-production-like-dev-auth/api.md)
- [Admin password authentication](../../specs/017-platform-admin-password-authentication/api.md)

Before shared staging writes, confirm the task authorizes that target and inspect existing content. Prefer stable-key APIs and explicit small changes. Verify via read APIs. Do not erase player progress or run broad SQL replacements to repair a seed.

## Make a minor API change

1. Trace controller -> command/query -> handler -> repository/service -> DTO/entity -> tests. Follow applicable local SprintLabs skills.
2. Inspect Unity callers before changing routes, envelopes, types or units. Prefer additive compatible changes.
3. Keep controllers thin and separate feature files. Reuse repositories for simple queries. Preserve authorization, tenant scope, validation, transactions and retry protections.
4. Update owning `specs/<feature>/api.md`, `frontend.md` and affected integration examples. Add focused tests for meaningful behavior changes.
5. Build/test and review the diff. Report skipped tests honestly. Avoid new packages or architectural rewrites for small edits.

Do not bypass reward/wallet services, accept client-selected identity as authority, regenerate educational UUIDs on retries or weaken permissions to unblock UI work.

## Integrate and test with Unity

Reuse existing Backend adapters/settings, AuthenticationSession and composition patterns. Follow `.asmdef` boundaries, preserve .meta GUIDs and inspect scene/prefab references. Runtime code must not reference UnityEditor.

Choose the backend API URL independently from the Mirror server address. For local persistence testing, client and trusted game server must target the same backend. On another machine, localhost means that machine; use an approved reachable development address. Do not change shared production defaults merely for local testing.

The API's `GameServer__ApiKey` must match the trusted Unity server's private credential, supplied through its existing launcher or `SPRINTLAB_GAME_SERVER_API_KEY`. Keep it out of player builds and logs. Use real backend development accounts rather than offline-only identities to test persistence.

Verify the changed scenario:

- **Setup:** login -> setup check -> options -> save -> reload profile/login. Community-managed users cannot overwrite setup.
- **Shop:** map bundled assets -> configure listing -> browse -> purchase with a funded test account -> retry the same requestId -> confirm one charge/receipt -> equip -> reload. Purchases spend persisted coins.
- **Questions/post-match:** fetch curriculum/grade/unit -> play -> complete -> inspect that player's match/question history and progression. Repeated completion must not duplicate rewards/history.
- **Missions:** inspect weekly/seasonal missions -> report trusted events -> check progress -> claim -> verify reward/history and duplicate-claim protection.

Rebuild the local game server after Unity server-code changes. Follow Unity's test rules; do not start a competing batch process against an open project. If native tests/rebuilds are left to the developer, list exact remaining steps instead of claiming they passed.

## Review, publish and update memory

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln --no-restore
git diff --check
git diff --stat
```

Review and commit only the task's files in each repo; open linked PRs when requested. **Backend pushes/merges into `development` trigger `.github/workflows/deploy-staging.yml`**, including tests, image build and staging container replacement. A feature-branch push is not a deployment. Follow the task's publication authorization; local integration work does not itself authorize deployment. Inspect Unity workflows separately.

After authorized deployment, verify CI and relevant API/schema/content state. Prefer additive API deployment before releasing a client that requires it. Record implemented, tested and deployed states separately.

Update backend `docs/ai/current-state.md` after material changes, owning feature docs when behavior changes, and Unity PROJECT_HANDOFF.md under its rules. Include outstanding manual checks and known limits. Never record credentials or personal player data.
