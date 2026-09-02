# API Guide: Production-Like Development Player Authentication

## Purpose and Scope

This backend-only feature exposes a safe account selector and a production-compatible login path for eight pre-seeded development players. It replaces Google/Firebase verification only. The returned token and `LoginResponse` use the existing player authentication architecture.

Exact wire examples and error tables are in [the HTTP contract](contracts/development-authentication-api.md).

## Configuration

| Setting | Default | Meaning |
|---|---:|---|
| `DevelopmentAuthentication:Enabled` | false | Makes the two endpoints eligible in a non-Production environment |
| `DevelopmentAuthentication:SeedPlayers` | false | Allows startup to ensure the eight players; effective only when Enabled |
| `DevelopmentAuthentication:ApiKey` | absent | When non-whitespace, requires `X-SprintLabs-Dev-Key` on both endpoints |

Production unconditionally disables endpoints and seeding. Do not place `ApiKey` in committed settings. Use .NET User Secrets/environment variables locally and deployment-managed secrets on shared hosts.

The committed non-secret defaults are in `API/appsettings.Development.json`; both flags are `false`, and no API-key value is present.

## Endpoints

### `GET /api/v1/Account/development-players`

- Auth: development guard; optional configured `X-SprintLabs-Dev-Key`; no bearer token.
- Request body: none.
- Success: `BaseResponse<List<DevelopmentPlayerResponse>>` containing only `accountKey` and `displayName`.
- Common errors: 401 key failure, 404 disabled/Production, 409 invalid seed state.
- Consumer note: treat the response as an all-or-nothing selector. Do not cache it as proof that login will remain enabled.

### `POST /api/v1/Account/development-login`

- Auth: development guard; optional configured `X-SprintLabs-Dev-Key`; no bearer token.
- Request body: `{ "accountKey": "dev-player-01" }`.
- Success: existing `BaseResponse<LoginResponse>`.
- Common errors: 400 invalid shape/key, 401 key failure, 403 invalid account state, 404 disabled/Production/unknown, 409 inconsistent persisted pair.
- Consumer note: store/use the returned result exactly as Firebase login. Do not synthesize or override IDs.

### Existing `GET /api/v1/Users/me`

No change. Send `Authorization: Bearer <accessToken>` from development-login. It returns the trusted database UserId/PlayerProfileId represented by existing claims and the current database relationship.

## Security and Logging

- The header key authorizes use of development endpoints; it is not a player credential.
- Never place the header key in a URL, response, JWT claim, account record, or log.
- Safe logs may contain canonical account key and resolved IDs after authorization.
- Logs must not include access tokens, request authorization headers, provider tokens, connection strings, or exception details containing secrets.
- Shared development/staging hosts should also use network-level access controls.

## Database Behavior

- Startup seeding uses real Identity and EF Core infrastructure.
- Login and discovery are read-only for User/Player state.
- No migration is required.
- Repeated seeding preserves generated IDs and accumulated progression.
- Unknown login keys do not create rows.
