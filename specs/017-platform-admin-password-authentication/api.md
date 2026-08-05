# Authentication API Guide

## Password login

`POST /api/v1/Account/community-login` is the only password-login route for Platform Admins, Community Admins, and Teachers. It is anonymous and accepts:

```json
{
  "identifier": "username-or-email",
  "password": "password"
}
```

The backend trims and Identity-normalizes `identifier`, applies the configured Identity lockout/password rules, and derives authorization only from persisted data. It never accepts a role, community, or administrator flag.

Successful responses contain user details, access/refresh tokens and UTC expirations, and one public role:

- `PlatformAdmin` for a persisted active `IsPlatformAdmin` user. `community` is omitted and no membership is required.
- `CommunityAdmin` for exactly one active persisted Owner membership. `community` contains that membership's community.
- `Teacher` for exactly one active persisted Teacher membership. `community` contains that membership's community.

Unknown users, invalid passwords, passwordless users, inactive users, and unsupported memberships all receive the same generic HTTP 401 credential error. A locked user receives the existing safe HTTP 423 response. Multiple active Owner/Teacher memberships receive HTTP 409 rather than allowing an arbitrary role/community selection. Login creates no users, memberships, profiles, licenses, or players.

## Community registration

`POST /api/v1/Account/community-register` is anonymous and completes either a pending Owner or Teacher invitation.

```json
{
  "token": "invitation-token",
  "name": "User name",
  "password": "password"
}
```

The token resolves the user, role, email, and community. The request accepts no email, community ID, role, or privilege field. It uses the configured Identity password policy, activates the pending membership, and returns no token; sign in afterwards using `community-login`. Invalid, expired, revoked, or used tokens return the existing safe invitation error.

## Shared session and recovery routes

`forgot-password`, `reset-password`, `refresh-token`, and `logout` retain their existing shared contracts. Eligible Platform Admins, Community Admins, and Teachers use the same reset, hashed refresh-token rotation, and logout infrastructure. Google login and external-login records are unchanged.

## Frontend notes

- Use the returned role only for UI routing; backend authorization remains based on trusted persisted state.
- Never send or log a role, community ID, password, access token, refresh token, or invitation token outside the intended request.
- Do not display a community selector for a Platform Admin session.
