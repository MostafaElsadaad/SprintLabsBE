# B2C Player Profile API

## GET /api/v1/Users/me/player-profile

Returns the authenticated user's own player profile.

- Auth: bearer token required.
- Roles: no community role required.
- CommunityUser requirement: none.
- StudentLicense requirement: none.
- Request body: none.

Success response:

```json
{
  "data": {
    "id": 100,
    "name": "B2C Player",
    "email": "b2c@example.com",
    "pictureUrl": "https://example.com/avatar.png",
    "gold": 5,
    "experience": 50,
    "level": 2,
    "schoolName": "Optional School Name",
    "age": 10,
    "grade": 5
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Common errors:

- 401: missing or invalid bearer token.
- 403: authenticated user is suspended.
- 404: current user or current player's profile was not found.

Frontend usage notes:

- Use this endpoint for the current player's profile screen.
- Do not send or store a player profile id to fetch another profile.
- This endpoint is valid for B2C users with no community memberships or student licenses.

## PATCH /api/v1/PlayerProfiles/me

Updates editable fields on the authenticated user's own player profile.

- Alias: `/api/v1/player-profiles/me`.
- Auth: bearer token required.
- Roles: no community role required.
- CommunityUser requirement: none.
- StudentLicense requirement: none.
- Request body fields: `age`, `grade`, `schoolName`.

Request body:

```json
{
  "age": 10,
  "grade": 5,
  "schoolName": "Optional School Name"
}
```

Nullable field behavior:

- Omitted fields are left unchanged.
- Fields sent as `null` are cleared.
- `age` must be null or between 1 and 120.
- `grade` must be null or between 1 and 20.
- `schoolName` must be null or no more than 200 characters.

Success response:

```json
{
  "data": {
    "id": 100,
    "name": "B2C Player",
    "email": "b2c@example.com",
    "pictureUrl": "https://example.com/avatar.png",
    "gold": 5,
    "experience": 50,
    "level": 2,
    "schoolName": "Optional School Name",
    "age": 10,
    "grade": 5
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Common errors:

- 400: invalid `age`, `grade`, `schoolName`, or JSON value type.
- 401: missing or invalid bearer token.
- 403: authenticated user is suspended.
- 404: current user or current player's profile was not found.

Frontend usage notes:

- Use this endpoint for B2C profile editing.
- Do not include user id or player profile id in the route or body.
- The server scopes the update from the JWT `userId` claim.

## Analytics Context

This feature does not emit or calculate analytics.

Future analytics should treat B2C profile/game activity as `CommunityId = null`. B2B school analytics should use the relevant `communityId` from the school/community context.
