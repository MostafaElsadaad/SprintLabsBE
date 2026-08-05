# API Contract: Invite-Only Teacher Authentication

All response bodies continue to use the repository's `BaseResponse<T>` envelope. Paths below show API version 1; the controller version placeholder remains authoritative. Feature error codes are `TeacherAlreadyBelongsToAnotherCommunity = 4`, `InvalidTeacherInvitation = 5`, and `InvalidOrExpiredPasswordResetToken = 6`.

## Removed public endpoints

These routes must not resolve and must not appear in Swagger:

| Method | Route |
|---|---|
| POST | `/api/v1/Account/teachers/register` |
| POST/GET as currently defined | `/api/v1/Account/confirm-email` |
| POST | `/api/v1/Account/resend-confirmation` |
| POST | Existing authenticated community invitation acceptance route |

No replacement public account-creation or confirmation API is introduced.

## POST `/api/v1/Account/community-login`

**Authentication**: Anonymous

```json
{
  "identifier": "username-or-email",
  "password": "password"
}
```

Identifier matches normalized email or normalized username. Platform admins return from persisted `IsPlatformAdmin` without a community. Community members require password, confirmed email, no suspension/lockout, exactly one Active Owner or Teacher membership, no Pending membership, and an active linked community. Lockout-on-failure remains enabled.

**Success data**:

```json
{
  "userId": "uuid",
  "name": "Teacher Name",
  "email": "teacher@example.com",
  "role": "Teacher",
  "community": {
    "id": "uuid",
    "name": "Community Name"
  },
  "accessToken": "opaque-to-client",
  "accessTokenExpiresAt": "2026-08-02T12:00:00Z",
  "refreshToken": "opaque-to-client",
  "refreshTokenExpiresAt": "2026-08-09T12:00:00Z"
}
```

**Errors**:

- `400`/`401` existing generic invalid-credentials response for unknown identifier, wrong password, incomplete/pending setup, no valid community, multiple current communities, inactive community, suspended, or locked account. The response must not identify which condition applied.
- Existing validation envelope for missing identifier/password.

No player profile, student license, or community selection is created.

## POST `/api/v1/Communities/{communityId}/teachers/invite`

**Authentication**: Bearer; caller must be an Active Owner of the active route community.

```json
{
  "email": "teacher@example.com"
}
```

Email is trimmed/normalized by the backend. Name is not accepted because the invited teacher supplies it during completion.

**Success**: Existing `BaseResponse` success contract. For a new invite, creates/reuses the eligible teacher user, one Pending Teacher membership, one seat reservation, and one usable invitation. For a same-community resend, reuses the membership/reservation and invalidates the prior token before issuing a new one.

**Errors**:

- `401`: unauthenticated.
- `403`: caller is not an Active Owner of the community.
- `404` or existing safe not-found response: community unavailable under current conventions.
- `409`, `TeacherAlreadyBelongsToAnotherCommunity`: the normalized email has an Active/Pending Teacher relationship with another community; no state is changed.
- Existing validation/capacity/incompatible-account-type errors.

The API never returns the raw invitation token. It is emailed in:

```text
{Frontend.BaseUrl}/invitations/teacher/setup?token={urlEncodedToken}
```

## GET `/api/v1/community-invitations/teacher/validate?token={rawToken}`

**Authentication**: Anonymous

**Success data**:

```json
{
  "communityName": "Community Name",
  "maskedEmail": "y***a@example.com",
  "expiresAt": "2026-08-03T12:00:00Z"
}
```

Validation is read-only and never returns the token, full email, user ID, password data, sender data, or membership ID.

**Errors**:

- `400` with stable `InvalidTeacherInvitation` for missing, invalid, expired, revoked, used, inconsistent, or otherwise unusable invitations. Detailed token state is not exposed.

## POST `/api/v1/Account/community-register`

**Authentication**: Anonymous

```json
{
  "token": "raw-invitation-token",
  "name": "Teacher Name",
  "password": "StrongPassword123!"
}
```

Email, community, and Owner/Teacher role are derived from the invitation and cannot be submitted or changed. The same endpoint completes Owner and Teacher invitations.

**Success**: Existing `BaseResponse` success contract. Sets trimmed name, sets deterministic email username if absent, sets password through Identity, confirms email, activates the existing Pending membership, and marks the invitation used. It does not log in or return tokens.

**Errors**:

- `400`, `InvalidTeacherInvitation`: invalid, expired, revoked, used/replayed, or inconsistent invitation.
- Existing validation response for blank name or password-policy failure.
- `409`, `TeacherAlreadyBelongsToAnotherCommunity`: another Active/Pending Teacher relationship is found during the authoritative recheck; no relationship is moved.

Repeated or concurrent completion has one successful winner; later attempts receive the safe invalid-invitation response. Completion never creates another membership, player profile, student license, or counter increment.

## POST `/api/v1/Account/forgot-password`

**Authentication**: Anonymous

```json
{
  "identifier": "username-or-email"
}
```

**Response**: Always HTTP 202 for a syntactically accepted request, whether the account is unknown, ineligible, cooling down, or email delivery fails.

```json
{
  "message": "If a matching account exists, password reset instructions have been sent."
}
```

Only an eligible completed teacher with a password and one active community receives mail. The link is:

```text
{Frontend.BaseUrl}/reset-password?userId={userId}&token={urlEncodedToken}
```

Raw tokens are not persisted or logged.

## POST `/api/v1/Account/reset-password`

**Authentication**: Anonymous

```json
{
  "userId": "uuid",
  "token": "url-safe-reset-token",
  "newPassword": "StrongPassword123!"
}
```

**Success**: Existing success envelope. Identity password policy is enforced and all active refresh tokens are revoked. No login or membership change occurs.

**Errors**:

- `400`, `InvalidOrExpiredPasswordResetToken`: invalid user/token or invalid/expired/used token without revealing account details.
- Existing safe password-policy validation errors.

## Preserved contracts

- Refresh-token rotation and logout endpoints, inputs, outputs, and behavior remain unchanged.
- Access/refresh token serialization remains unchanged.
- Google authentication endpoints, handlers, DTOs, validation, claims, linking, and behavior remain unchanged.
- Firebase/player and community-owner authentication are outside this contract and remain unchanged.
