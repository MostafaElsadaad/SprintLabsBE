# Quickstart: Production-Like Development Player Authentication

## Purpose

After implementation, validate safe configuration, idempotent seeding, account discovery, real SprintLabs login, `/Users/me` identity equality, Production refusal, Firebase regression, and secret hygiene.

## Prerequisites

- .NET 8 SDK.
- A non-Production SprintLabs backend environment.
- MySQL reachable through secret/environment configuration for manual validation.
- No Google/Firebase credential is needed for development-login.

## Local Configuration

Prefer .NET User Secrets from the API project:

```powershell
dotnet user-secrets --project API set "DevelopmentAuthentication:Enabled" "true"
dotnet user-secrets --project API set "DevelopmentAuthentication:SeedPlayers" "true"
dotnet user-secrets --project API set "DevelopmentAuthentication:ApiKey" "<local-secret>"
```

Or use process environment variables:

```powershell
$env:DevelopmentAuthentication__Enabled = "true"
$env:DevelopmentAuthentication__SeedPlayers = "true"
$env:DevelopmentAuthentication__ApiKey = "<local-secret>"
```

Never commit the key. `ApiKey` may be omitted on a trusted local backend; then the header is not required. The feature remains disabled unless `Enabled=true`, and Production remains denied even when all values are set.

`API/appsettings.Development.json` contains only the committed non-secret disabled defaults. Enable the feature through User Secrets or environment/deployment configuration rather than changing committed settings.

## Seed Validation

1. Start the API in Development with both flags enabled.
2. Confirm safe logs report that eight accounts were ensured without tokens, keys, connection strings, or passwords.
3. Record the eight UserId/PlayerProfileId pairs from a trusted database/test inspection.
4. Restart at least twice.
5. Confirm exactly eight catalog accounts remain, all identifiers are unchanged, and no progression values were reset.

## Discover Accounts

```powershell
$devHeaders = @{ "X-SprintLabs-Dev-Key" = "<local-secret>" }
$players = Invoke-RestMethod -Method Get -Uri "https://localhost:<port>/api/v1/Account/development-players" -Headers $devHeaders
$players.data
```

Expect exactly `dev-player-01` through `dev-player-08` in order, with display names only. Omit `-Headers` when no API key is configured.

## Login and Validate `/Users/me`

```powershell
$body = @{ accountKey = "dev-player-01" } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "https://localhost:<port>/api/v1/Account/development-login" -Headers $devHeaders -ContentType "application/json" -Body $body
$meHeaders = @{ Authorization = "Bearer $($login.data.accessToken)" }
$me = Invoke-RestMethod -Method Get -Uri "https://localhost:<port>/api/v1/Users/me" -Headers $meHeaders
```

Confirm:

- login returned existing LoginResponse fields;
- the token was accepted without a development header;
- `$login.data.userId -eq $me.data.userId`;
- `$login.data.playerProfileId -eq $me.data.playerProfileId`;
- repeated login preserves both IDs;
- `dev-player-02` returns a different pair.

## Negative Checks

- Unknown key: controlled rejection and unchanged User/Player counts.
- Uppercase/whitespace key: rejection; it must not select another account.
- Missing/incorrect/duplicate configured header: generic 401 and no account data/token.
- Disabled `Enabled`: no endpoint exposure and no seed invocation.
- Enabled with `SeedPlayers=false`: endpoints may be available, but startup creates nothing and unseeded login fails.
- `ASPNETCORE_ENVIRONMENT=Production` with both flags/key set: no seeding and both endpoints remain unavailable.
- Suspended/locked selected User or missing/mismatched Player: no token and no repair.

## Automated Validation

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~DevelopmentPlayerAuthentication|FullyQualifiedName~FirebasePlayerAuthentication"
dotnet test SprintLabs.sln
```

The focused end-to-end test must call development-login and then the real `/api/v1/Users/me` route through JWT bearer middleware using an isolated in-memory store. Automated tests must not contact Firebase, Google, or a developer database.

## Secret and Diff Inspection

```powershell
git diff --check
git diff -- API Shared Application Domain Infrastructure SprintLabs.Tests specs/020-production-like-dev-auth
git diff -- . ':!specs/020-production-like-dev-auth' | Select-String -Pattern 'DevelopmentAuthentication__ApiKey|X-SprintLabs-Dev-Key.*:|accessToken.*eyJ|Password=' -CaseSensitive:$false
```

Review the actual feature diff manually as well. Confirm no new development key, JWT, database credential, connection string, password, or provider token was added or logged. Pre-existing repository secrets are a separate remediation concern and must not be copied into feature artifacts.

## Firebase Regression

Run existing Firebase controller, handler, workflow, persistence, and activation tests. If a valid Firebase test environment is available, manually confirm `/api/v1/Account/firebase-login` still returns the unchanged response and its token still reaches `/Users/me`.

See [the plan](plan.md), [data model](data-model.md), [HTTP contract](contracts/development-authentication-api.md), [API guide](api.md), and [consumer boundary](frontend.md).
