# Authentication Contract

All routes use version 1 and `BaseResponse<T>`. Platform admins are seeded through trusted backend configuration; there is no public administrator registration.

## POST `/api/v1/Account/community-login`

Anonymous request:

```json
{ "identifier": "username-or-email", "password": "password" }
```

The backend normalizes the identifier, performs Identity password verification/lockout handling, and derives the role from persisted data. Success data contains `userId`, `name`, `username`, `email`, `role`, `accessToken`, `accessTokenExpiresAt`, `refreshToken`, and `refreshTokenExpiresAt`.

- A persisted active platform admin returns `role: "PlatformAdmin"` and no `community`.
- One active Owner membership returns `role: "CommunityAdmin"` and `community: { id, name }`.
- One active Teacher membership returns `role: "Teacher"` and `community: { id, name }`.

Credential failures are deliberately generic (HTTP 401). Lockout uses the existing HTTP 423 response. Conflicting active community memberships fail with HTTP 409. No role or community can be selected by the request.

## POST `/api/v1/Account/community-register`

Anonymous request:

```json
{ "token": "invitation-token", "name": "User Name", "password": "password" }
```

The existing invitation service derives an eligible pending Owner or Teacher membership from the hashed token, uses Identity to set the password, confirms the invited email, activates that membership, and returns no session. Role, email, community ID, and privilege fields are not accepted.

## Preserved shared contracts

Forgot/reset password, refresh-token rotation, logout, Google login, and Google external-login records retain their existing contracts and services. Password reset revokes sessions; login and registration never log raw credentials or tokens.
