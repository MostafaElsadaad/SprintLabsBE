# User Identity Foundation API

## Feature Summary

This feature provides the shared authenticated `User` identity used by Sprint Labs while preserving the existing game `Player` profile. Google game login creates or reuses both records, and authenticated clients can load the current identity and linked player profile.

All routes are API version 1. Successful responses use camel-cased `BaseResponse<T>` JSON:

```json
{
  "data": {},
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`statusCode` and `errorCode` are numeric enum values. Controlled handler failures omit `data`. Authentication middleware may return an empty 401 response instead of a `BaseResponse`.

## Endpoint List

| Method | Route | Authentication | Purpose |
|---|---|---|---|
| POST | `/api/v1/Account/google-login` | Public; valid Google ID token required | Sign in and create/reuse the shared user and player profile |
| GET | `/api/v1/Users/me` | Bearer JWT | Load the current user identity |
| GET | `/api/v1/Users/me/player-profile` | Bearer JWT | Load the current user's linked player profile |

## POST /api/v1/Account/google-login

### Request

The current controller binds `googleAccessToken` from the query string. It does not accept a JSON request body.

```http
POST /api/v1/Account/google-login?googleAccessToken=GOOGLE_ID_TOKEN
```

The value is validated as a Google ID token against the configured allowed client IDs.

### Success Response

```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzUxMiIs...",
    "userId": 42,
    "playerProfileId": 18,
    "name": "Ada Player",
    "email": "ada@example.com",
    "pictureUrl": "https://example.com/avatar.png",
    "gold": 0,
    "experience": 0,
    "level": 1
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The JWT is valid for seven days and includes Google identity claims plus `userId` and `playerProfileId`.

### Common Errors

| HTTP | Typical condition | Response behavior |
|---|---|---|
| 400 | Missing token, invalid Google token surfaced outside the handler, user creation/update failure | Controlled/global error response; exact message depends on the thrown exception |
| 401 | Google identity is missing required email or Google subject | `InvalidAccessToken` controlled error |
| 403 | The matched user is suspended | `InvalidAccessToken` controlled error |
| 409 | An existing player is already linked to a different user | `Record Already Exists` controlled error |

Example controlled error:

```json
{
  "message": "InvalidAccessToken",
  "statusCode": 403,
  "errorCode": 3
}
```

### Frontend Usage Notes

- Obtain a Google ID token, not an OAuth authorization code.
- URL-encode the token when adding it to the query string.
- Store `accessToken` using the frontend's secure session strategy.
- Use `userId` as the shared identity key and `playerProfileId` as the game-profile key.
- Existing game progression fields remain available directly in the login response.
- A successful game login always creates or finds a player profile, so `playerProfileId` is populated in the implemented flow.

## GET /api/v1/Users/me

### Authentication

Send:

```http
Authorization: Bearer YOUR_SPRINTLABS_JWT
```

The controller resolves the exact `userId` claim from the JWT.

### Success Response

```json
{
  "data": {
    "userId": 42,
    "email": "ada@example.com",
    "name": "Ada Player",
    "avatarUrl": "https://example.com/avatar.png",
    "status": "Active",
    "isPlatformAdmin": false,
    "playerProfileId": 18
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`avatarUrl` and `playerProfileId` may be `null`.

### Common Errors

| HTTP | Condition |
|---|---|
| 401 | Missing/invalid bearer token, or JWT has no parseable `userId` claim |
| 403 | User status is `Suspended` |
| 404 | JWT references a user that no longer exists |

### Frontend Usage Notes

- Use this endpoint as the authenticated application bootstrap call.
- Gate platform-admin navigation with `isPlatformAdmin`.
- Gate game-profile navigation with a non-null `playerProfileId`.
- Treat `status` as a display/decision string; currently supported values are `Active` and `Suspended`.

## GET /api/v1/Users/me/player-profile

### Authentication

Bearer JWT required. No request body or query parameters.

### Success Response

```json
{
  "data": {
    "id": 18,
    "name": "Ada Player",
    "email": "ada@example.com",
    "pictureUrl": "https://example.com/avatar.png",
    "gold": 120,
    "experience": 450,
    "level": 7,
    "schoolName": "Example School",
    "age": 12,
    "grade": 6
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`pictureUrl`, `schoolName`, `age`, and `grade` may be `null`.

### Common Errors

| HTTP | Condition |
|---|---|
| 401 | Missing/invalid bearer token, or missing `userId` claim |
| 403 | User is suspended |
| 404 | User does not exist or has no linked player profile |

### Frontend Usage Notes

- Call after login when full player progression/profile data is needed.
- A 404 is a valid "no player profile" state for non-game identities.
- Do not request profiles by arbitrary user or player IDs; this endpoint is always scoped to the JWT user.

## Manual Test Steps

1. Obtain a valid Google ID token for an allowed client ID.
2. Call `POST /api/v1/Account/google-login?googleAccessToken=...`.
3. Record `data.userId`, `data.playerProfileId`, and `data.accessToken`.
4. Repeat the login and confirm the same user and player-profile IDs are returned.
5. Call `GET /api/v1/Users/me` with the returned bearer token.
6. Confirm the identity fields and player-profile ID match the login response.
7. Call `GET /api/v1/Users/me/player-profile` and confirm its `id` matches `playerProfileId`.
8. Remove the bearer token and confirm current-user endpoints return 401.
9. Where test data permits, use a suspended user and confirm identity reads return 403.

## Implementation Notes / Spec Differences

- The implemented Google login route is versioned as `/api/v1/Account/google-login`, not the unversioned route used in some feature wording.
- `googleAccessToken` is a query-string parameter despite the command property being named `IdToken`.
- Missing display name does not fail login; the user service falls back to the email as the name.
- Some invalid Google-token exceptions are handled by the global fallback and may return 400 rather than the spec's expected 401.
- Focused identity tests listed in the original tasks remain unchecked in `tasks.md`.

## Open Questions

- Should Google login move the token to a JSON body to avoid placing a credential-like value in URLs and logs?
- Should all Google token validation failures be normalized to the same 401 `BaseResponse`?
- Should the frontend receive a dedicated error code distinguishing "user not found" from "player profile not found"?
