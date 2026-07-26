# HTTP Contract: Teacher Email Authentication

## Conventions

- API version: `v1`.
- JSON properties use camel case.
- Success responses use `BaseResponse<T>`:

```json
{
  "data": {},
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

- Success without data uses `BaseResponse`.
- Controlled errors contain `message`, numeric `statusCode`, and numeric `errorCode`; they do not echo passwords or tokens.
- Bearer-authenticated operations require a JWT with a parseable `userId` claim.
- No client request may supply the authenticated user ID or network address.

## Endpoint Matrix

| Method | Route | Authentication | Success |
|---|---|---|---:|
| POST | `/api/v1/Account/teachers/register` | Public | 202 |
| POST | `/api/v1/Account/confirm-email` | Public | 200 |
| POST | `/api/v1/Account/resend-confirmation` | Public | 202 |
| POST | `/api/v1/Account/teachers/login` | Public | 200 |
| POST | `/api/v1/Account/refresh-token` | Public; refresh token required | 200 |
| POST | `/api/v1/Account/logout` | Public; refresh token required | 200 |
| POST | `/api/v1/Account/forgot-password` | Public | 202 |
| POST | `/api/v1/Account/reset-password` | Public | 200 |
| POST | `/api/v1/Communities/{communityId}/teachers/invite` | Bearer; Active Owner in route community | 200 |
| POST | `/api/v1/CommunityInvitations/accept` | Bearer; confirmed Teacher account | 200 |
| GET | `/api/v1/Users/me/communities` | Bearer | 200; unchanged |

## POST /api/v1/Account/teachers/register

Request:

```json
{
  "name": "Jane Smith",
  "email": "jane@example.com",
  "password": "StrongPass123!"
}
```

Success:

```json
{
  "data": {
    "resendAvailableAt": "2026-07-25T12:01:00Z"
  },
  "message": "Registration successful. Please check your email to confirm your account.",
  "statusCode": 202,
  "errorCode": 1
}
```

Errors:

- `400`: invalid name/email or password-policy failure.
- `409`: normalized email belongs to an ineligible existing Google/player account or an existing password account that cannot be reused.
- `503`: account state was committed but confirmation email delivery failed; resend is available after the returned/persisted cooldown.

## POST /api/v1/Account/confirm-email

Request:

```json
{
  "userId": 123,
  "token": "base64url-identity-token"
}
```

Success:

```json
{
  "message": "Email confirmed successfully.",
  "statusCode": 200,
  "errorCode": 1
}
```

Repeated confirmation for the same account returns the same safe success. Invalid, malformed, expired, or wrong-user tokens return a generic `400`.

## POST /api/v1/Account/resend-confirmation

Request:

```json
{
  "email": "jane@example.com"
}
```

Success:

```json
{
  "data": {
    "resendAvailableAt": "2026-07-25T12:02:00Z"
  },
  "message": "If the account requires confirmation, a confirmation email has been sent.",
  "statusCode": 202,
  "errorCode": 1
}
```

The same public shape is returned for unknown and already-confirmed accounts. An eligible account inside cooldown receives no email. SMTP failure returns `503` without account or token details.

## POST /api/v1/Account/teachers/login

Request:

```json
{
  "email": "jane@example.com",
  "password": "StrongPass123!"
}
```

Success:

```json
{
  "data": {
    "userId": 123,
    "name": "Jane Smith",
    "email": "jane@example.com",
    "accountType": "Teacher",
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-07-25T12:15:00Z",
    "refreshToken": "raw-single-use-token",
    "refreshTokenExpiresAt": "2026-08-24T12:00:00Z",
    "communities": [
      {
        "communityId": 10,
        "communityName": "Alpha School",
        "role": "Teacher"
      }
    ]
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`communities` is `[]` when the teacher has no active membership. Pending and Removed memberships are excluded.

Errors:

- `401`: generic invalid teacher credentials, including unknown/non-teacher/passwordless/wrong-password cases.
- `403`: confirmed credentials identify an unconfirmed or suspended teacher.
- `423`: account is locked.

No access or refresh token appears in any error.

## POST /api/v1/Account/refresh-token

Request:

```json
{
  "refreshToken": "raw-single-use-token"
}
```

Success:

```json
{
  "data": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-07-25T12:30:00Z",
    "refreshToken": "new-raw-single-use-token",
    "refreshTokenExpiresAt": "2026-08-24T12:15:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Unknown, expired, revoked, reused, or suspended-user tokens return `401`. The old token is unusable after success.

## POST /api/v1/Account/logout

Request:

```json
{
  "refreshToken": "raw-single-use-token"
}
```

Success, including a repeated logout:

```json
{
  "message": "Logged out successfully.",
  "statusCode": 200,
  "errorCode": 1
}
```

The operation never returns a replacement token.

## POST /api/v1/Account/forgot-password

Request:

```json
{
  "email": "jane@example.com"
}
```

Every validly shaped request returns:

```json
{
  "message": "If an account exists, a password reset email has been sent.",
  "statusCode": 202,
  "errorCode": 1
}
```

The response does not distinguish unknown, non-teacher, passwordless, suspended, or eligible accounts.

## POST /api/v1/Account/reset-password

Request:

```json
{
  "email": "jane@example.com",
  "token": "base64url-identity-token",
  "newPassword": "NewStrongPass123!"
}
```

Success:

```json
{
  "message": "Password reset successfully.",
  "statusCode": 200,
  "errorCode": 1
}
```

Invalid password policy or invalid/expired/malformed token returns `400`. Success revokes every active refresh token for the account.

## POST /api/v1/Communities/{communityId}/teachers/invite

Existing request:

```json
{
  "email": "jane@example.com",
  "name": "Jane Smith"
}
```

Existing response shape remains:

```json
{
  "data": {
    "userId": 123,
    "name": "Jane Smith",
    "email": "jane@example.com",
    "status": "Pending",
    "createdAt": "2026-07-25T12:00:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Behavior changes:

- Newly invited, restored, registered, or passwordless teachers remain `Pending`.
- Reissuing a Pending invitation preserves membership identity and teacher usage.
- Active membership returns idempotently without sending another invitation.
- Existing non-teacher Google/player identity conflict returns `409`; no account linking occurs.
- Full capacity/invalid data returns `400`; unauthorized route community returns `403`.
- SMTP failure returns `503` after the valid pending state is committed and can be resent.

## POST /api/v1/CommunityInvitations/accept

Request:

```json
{
  "token": "raw-invitation-token"
}
```

Success:

```json
{
  "data": {
    "communityId": 10,
    "communityName": "Alpha School",
    "role": "Teacher",
    "status": "Active"
  },
  "message": "Invitation accepted successfully.",
  "statusCode": 200,
  "errorCode": 1
}
```

Errors:

- `401`: missing/invalid bearer token.
- `403`: suspended, unconfirmed, non-teacher, or email-mismatched account.
- `400`: malformed/unknown token or membership no longer eligible.
- `410`: expired, revoked, or superseded invitation.

Repeating acceptance by the same matching teacher returns the existing Active membership with `200`; teacher-seat usage is unchanged.

## GET /api/v1/Users/me/communities

No contract change. It continues returning every active membership, including newly accepted Teacher memberships, and excludes Pending and Removed memberships.

