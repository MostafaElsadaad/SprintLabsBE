# HTTP Contract: Development Player Authentication

## Shared Rules

- Base route convention: `/api/v1/Account`.
- Both routes are bearer-anonymous because their purpose is to obtain a player token, but they are available only when `DevelopmentAuthentication.Enabled=true` and the runtime environment is not Production.
- If `DevelopmentAuthentication.ApiKey` is configured, send exactly one `X-SprintLabs-Dev-Key` header.
- Production and disabled configurations return controlled 404 behavior before catalog lookup.
- A missing, duplicate, or invalid configured header returns a generic 401 and never reveals the expected value.
- Success and error payloads use the existing `BaseResponse<T>` / `BaseResponse` JSON envelope.

## GET `/api/v1/Account/development-players`

Returns the safe selector entries for the complete valid seeded set.

### Request

```http
GET /api/v1/Account/development-players HTTP/1.1
X-SprintLabs-Dev-Key: <optional-configured-secret>
```

No body.

### Success: 200

```json
{
  "data": [
    {
      "accountKey": "dev-player-01",
      "displayName": "Dev Player 01"
    },
    {
      "accountKey": "dev-player-02",
      "displayName": "Dev Player 02"
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The actual successful list contains exactly eight items in canonical key order. It contains no UserId, PlayerProfileId, email, token, password, provider identity, API key, or database detail.

### Errors

| Status | Condition |
|---:|---|
| 401 | A configured key is missing, duplicated, or invalid |
| 404 | Feature disabled or runtime is Production |
| 409 | One or more catalog-backed User/Player pairs are missing or inconsistent |

The endpoint never seeds or repairs an account and never returns a partial list.

## POST `/api/v1/Account/development-login`

Issues the existing SprintLabs player login result for one already seeded pair.

### Request

```http
POST /api/v1/Account/development-login HTTP/1.1
Content-Type: application/json
X-SprintLabs-Dev-Key: <optional-configured-secret>

{
  "accountKey": "dev-player-03"
}
```

`accountKey` is required and must exactly match a catalog key. No trimming or case folding selects another account.

### Success: 200

```json
{
  "data": {
    "accessToken": "<real-sprintlabs-jwt>",
    "userId": 123,
    "playerProfileId": 456,
    "name": "Dev Player 03",
    "email": "dev-player-03@development.sprintlabs.invalid",
    "pictureUrl": "",
    "gold": 0,
    "experience": 0,
    "level": 1
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The response type is the existing `LoginResponse`. IDs are database-generated 64-bit values. Progression fields reflect persisted values and may differ from first-seed defaults after gameplay.

### Errors

| Status | Condition |
|---:|---|
| 400 | Body/key is missing, malformed, or non-canonical |
| 401 | A configured development key is missing, duplicated, or invalid |
| 403 | The seeded account is suspended, currently locked, or otherwise invalid |
| 404 | Feature disabled, runtime Production, or account key unknown/unseeded |
| 409 | User/Profile relationship or deterministic identity is inconsistent |

Every failure issues no token and performs no User/Player create, link, update, or repair.

## Bearer Validation Proof

Use the returned `accessToken` unchanged:

```http
GET /api/v1/Users/me HTTP/1.1
Authorization: Bearer <accessToken>
```

The unchanged route validates the configured SprintLabs issuer, audience, signature, and lifetime, then returns the same `userId` and `playerProfileId` values. No development header, claim, or Mirror exception is required downstream.
