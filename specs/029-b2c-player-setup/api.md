# B2C player setup API

Implementation branch: `codex/b2c-player-setup`. Requires migration `20261028230451_AddB2CPlayerSetup`. Not deployed by this task.

All endpoints use existing SprintLabs JWT bearer authentication and API version 1. JWT `userId` selects the player; do not submit a user or player ID. Routes also accept the existing `player-profiles` controller alias. Every success uses the existing envelope `data`, `message`, `statusCode`, `errorCode` (Success = 1).

## Check after login

**GET `/api/v1/PlayerProfiles/me/setup`** — authenticated player. No body.

```json
{
  "data": {
    "playerProfileId": 23,
    "nickname": "Space Explorer",
    "grade": 7,
    "curriculum": "Egyptian National",
    "language": "English",
    "isCommunityManaged": false,
    "requiresSetup": false
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`requiresSetup` is true for a B2C profile with a blank name, grade outside 7–12, blank curriculum, or blank language. Existing provider-created names alone do not complete setup. First-time curriculum/language are null. Call after each successful login/session restore; no new token claim or change to existing login/refresh contracts.

Community management is determined server-side by a non-revoked student license matching the player ID, user ID, or email (including pending invitations). A community-managed player always returns `isCommunityManaged=true`, `requiresSetup=false`; community-managed data continues to come from the community flow. This feature does not implement new B2B education mappings.

## Dropdown choices

**GET `/api/v1/PlayerProfiles/setup-options`** — authenticated. No body.

```json
{
  "data": {
    "grades": [7, 8, 9, 10, 11, 12],
    "educationOptions": [
      { "curriculum": "Egyptian National", "language": "Arabic" },
      { "curriculum": "Egyptian National", "language": "English" },
      { "curriculum": "IG", "language": "English" }
    ]
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Actual options are the distinct union of the seeded pairs and curriculum/language pairs in the new Questions table for grades 7–12. Values are trimmed, sorted and deduplicated. Submit the exact returned strings. The migration inserts the three examples above; no fake questions are inserted. Grades are the supported values, not community Grade row IDs. A selectable example does not guarantee questions exist for every grade/unit.

## Save setup

**POST `/api/v1/PlayerProfiles/me/setup`** — authenticated B2C player only.

```json
{
  "nickname": "Space Explorer",
  "grade": 7,
  "curriculum": "Egyptian National",
  "language": "English"
}
```

Returns HTTP 200 with the same setup envelope above and `requiresSetup=false` after successful persistence. All four values are required. Nickname is trimmed, 1–100 characters, no control characters; duplicates allowed. Grade must be an integer 7–12. Curriculum (max 120) and language (max 40) must be a returned pair. Strings are trimmed before comparison. Null, blank, unknown choices and wrong types are rejected. Validation finishes before any player field is changed.

Nickname is the existing `Players.Name`, not a second name column. Curriculum and language are new nullable columns; grade reuses `Players.Grade`. Saving does not change account/legal/Google name, XP, gold, rank, pictures or community records. Repeating the same form is safe; later valid B2C form saves may update these four fields. There is no separate completed timestamp/flag. A normal save requires one database SaveChanges operation. Existing GET `/api/v1/Users/me/player-profile` also returns the new `curriculum` and `language` and the selected nickname as `name`.

## Errors

- 400: invalid input/pair/grade/missing values. Controlled validation envelope: `{"message":"Invalid Data","statusCode":400,"errorCode":3}`. Malformed JSON/type binding may return ASP.NET validation ProblemDetails instead; handle by HTTP status.
- 401: missing/expired JWT or invalid user ID claim; authentication may return an empty body.
- 403: suspended/locked account, or a community-managed player attempts to save setup. Existing controlled forbidden envelope uses `message: "InvalidAccessToken"`, statusCode 403, errorCode 3; use the GET management flag to decide UI, not the message text.
- 404: signed-in account/player record unavailable; do not create a fake player locally.

## Local/deployment verification

Run `dotnet build SprintLabs.sln`, then `dotnet test SprintLabs.sln`. Apply the migration through the normal deployment process before calling the APIs against a database. Never put JWTs/database credentials in the integration document or game assets. No staging schema/data writes or deployment were performed by this task.
