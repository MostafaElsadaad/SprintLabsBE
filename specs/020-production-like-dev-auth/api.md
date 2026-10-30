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

## 2026-10-30 - Explicit development-player school assignment

`POST /api/v1/admin/development-players/{accountKey}/school`

Operator-only setup for the eight existing catalogue accounts; this is not automatic
login, startup seeding, a production enrollment route, or a shipped Unity operation.
It requires an active, unlocked platform administrator bearer session AND the existing
`X-SprintLabs-Dev-Key` guard when configured. DevelopmentAuthentication must be enabled;
Production is always refused. Missing bearer authentication returns 401 before the handler.

Request (IDs are illustrative; discover existing valid school/grade/class IDs first):

```json
{"communityId":1,"gradeId":1,"classId":1}
```

Only canonical existing dev-player-01 through dev-player-08 may be targeted. Backend
catalogue lookup resolves both user/profile IDs. Client-supplied identity fields are ignored.
No account/profile is provisioned or repaired. Provider-linked, privileged, inactive,
locked or inconsistent targets are rejected. The school and class must be active and
belong to the requested grade/school; grade value must satisfy existing enrollment rules.

HTTP 200 uses BaseResponse with data:

```json
{
  "accountKey":"dev-player-01", "playerProfileId":40,
  "communityId":1, "gradeId":1, "classId":1,
  "studentLicenseId":123, "alreadyAssigned":false
}
```

The action creates one Active Student membership and an Active StudentLicense under the
existing serializable community enrollment transaction. A new license consumes one seat;
an existing matching Pending license activates without consuming another. Repeating the
same completed assignment returns the same license with alreadyAssigned=true, even if
capacity was subsequently reduced. It never moves a conflicting enrollment or changes
an Owner/Teacher membership into Student. Revoked licenses are not reactivated; creating
a replacement requires a free seat. No Google/Firebase identity, wallet or progression
fields are changed. AssignedByUserId records the caller for newly created licenses.

Errors: 400 invalid IDs/no available seats, 401 missing/invalid bearer or development key,
403 non-admin/inactive/locked actor or target, 404 disabled/Production/unknown test account
or inactive/foreign scope, 409 inconsistent identity/existing role/enrollment conflict,
503 transient database conflict. Retry the same account/school/grade/class tuple after 503;
do not blindly move accounts or grant seats. This endpoint does not send invitations.

After setup, log in normally as that development account, GET Users/me/communities and
school ranking/leaderboard to discover availableFilters, then verify each period and its
own class. School filters require the normal player's bearer; administrator success does
not prove player authorization. Use the existing owner-controlled revoke operation for
intentional cleanup; no new deletion or broad reset endpoint is added.

Local validation: 21 new handler/SQLite/HTTP cases pass, including Production/disabled/key
and ordinary-user denial, no identity override, capacity limits and repeat safety. Live
assignment requires deployment of this branch; no staging record is changed by these tests.
