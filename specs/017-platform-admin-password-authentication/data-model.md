# Data Model: Platform Administrator Password Authentication

## Model impact summary

No database model change is required. The feature reuses the existing Identity `User`, Identity external-login tables, and `RefreshToken`. `AuthenticatedAccountType`, platform-admin response DTOs, and rotated-token subject data are runtime-only contracts and are not persisted.

## User

`Infrastructure.DataAccess.User` remains the authoritative identity and administrator record.

### Existing relevant fields

| Field | Purpose |
|---|---|
| `Id` | Signed `userId` claim and owner of refresh/password state. |
| `UserName`, `NormalizedUserName` | Username response and case-insensitive identifier lookup. |
| `Email`, `NormalizedEmail` | Email response, lookup, and reset delivery. |
| `PasswordHash` | Identity-managed indication that a password exists; never assigned directly. |
| `EmailConfirmed` | Applied when current Identity sign-in policy requires confirmed email. |
| `AccessFailedCount`, `LockoutEnabled`, `LockoutEnd` | Existing failed-login and lockout state. |
| `SecurityStamp`, `ConcurrencyStamp` | Existing Identity password/session and concurrent-update protection. |
| `IsPlatformAdmin` | Persisted trusted platform-administrator source of truth. |
| `IsTeacherAccount` | Existing teacher eligibility; unchanged by this feature. |
| `Status` | Current Active/Suspended account state. Only Active administrators are eligible. |
| `GoogleId` | Existing active Google association; unchanged by password operations. |
| `LastPasswordResetEmailSentAt` | Existing enumeration-safe reset-email cooldown claim. |

### Existing indexes and constraints

- Unique normalized username index supplied by Identity.
- Unique normalized email index in the current model.
- Unique email under the repository's existing configuration.
- Existing Google identifier index.
- Identity password/security/concurrency fields already mapped.

### Platform-administrator invariants

- `IsPlatformAdmin` is set only by the existing trusted internal process and is never accepted from these endpoint bodies.
- A platform administrator requires no `CommunityUser` and no community selection.
- Password login requires Active status, an Identity password, non-lockout, and `EmailConfirmed` only when the active policy requires it.
- First-password setup targets the authenticated `Id`, requires Active persisted `IsPlatformAdmin`, and requires no existing password.
- First-password setup and reset change only Identity password/security/concurrency state plus refresh-token revocation; they do not change `IsPlatformAdmin`, `GoogleId`, Identity login rows, teacher state, player state, or community data.
- If `IsPlatformAdmin` and `IsTeacherAccount` are both true, shared refresh reconstructs PlatformAdmin as the trusted account type. Authorization still reloads persisted `IsPlatformAdmin`.

## Identity external-login association

The existing Identity login tables remain present through `IdentityDbContext`. The active Google implementation primarily associates the provider through `User.GoogleId` and `Player.GoogleId`.

### Invariants

- No new Google/external-login row is added by password login or setup.
- `AddPasswordAsync` and `ResetPasswordAsync` do not remove or replace external login rows.
- No call to add/remove/link/unlink a Google login is introduced.
- Existing Google user/player lookup and linking behavior remains unchanged.

## RefreshToken

The existing `Domain.Models.RefreshToken` remains the single renewable-session record.

| Field | Purpose |
|---|---|
| `UserId` | Associates the token with the persisted user used for current eligibility. |
| `TokenHash` | Unique SHA-256 hash of the raw refresh token. |
| `CreatedAt`, `ExpiresAt` | UTC issuance and expiration. |
| `RevokedAt` | UTC single-use/logout/reset/setup revocation. |
| `ReplacedByTokenHash` | Links successful rotation to the replacement hash. |
| `CreatedByIp`, `RevokedByIp` | Existing bounded network audit values. |

### Invariants

- Raw refresh tokens are returned only to the authenticated client and are never persisted or logged.
- A hash rotates at most once through conditional revocation.
- First-password setup and password reset revoke every active token for the user.
- Rotation requires current eligible persisted teacher or platform-admin state; it never trusts a requested account type.
- Platform-admin login uses the same table, hash, expiration, rotation, logout, and revoke-all behavior as teacher password login.

## Runtime-only account type

`AuthenticatedAccountType` contains only `Teacher` and `PlatformAdmin` and is not a database column.

- Password-login handlers select it from trusted Identity service results.
- Refresh rotation derives it from current persisted flags, with PlatformAdmin precedence.
- Access-token serialization uses its stable string representation.
- Client response/JWT account type never replaces the persisted authorization check.

## State transitions

```text
Existing Google-only platform admin
  (Active + IsPlatformAdmin + no password + Google association)
  -- authenticated self setup / valid password -->
Password-enabled platform admin
  (same user/admin/Google state + Identity password; prior refresh tokens revoked)

Password-enabled eligible platform admin
  -- username/email + correct password -->
Access token + hashed refresh-token record

Active refresh token
  -- rotate --> old token revoked + replacement token created
  -- logout --> token revoked
  -- first-password setup/reset for same user --> token revoked

Password-enabled platform admin
  -- forgot-password --> unchanged user + existing Identity reset credential emailed
  -- valid reset --> changed Identity password + all refresh tokens revoked
```

### Invalid transitions

- Unknown, non-admin, passwordless, suspended/non-active, policy-unconfirmed, or locked users receive no login token.
- Anonymous or non-admin callers cannot configure a password.
- Setup cannot target another user and cannot replace an existing password.
- Reset/setup cannot assign or remove administrator privilege or Google linkage.
- No operation transitions an administrator into a community, teacher, player, or student role.

## Transaction and lock ordering

Use the existing User row as the shared serialization point:

1. Begin a relational transaction.
2. Resolve and lock User by stable `Id`.
3. Recheck current administrator/account eligibility.
4. For setup/reset, perform the Identity password operation and revoke all active refresh tokens.
5. For rotation, reload/recheck the submitted refresh-token hash, conditionally revoke it, and insert its replacement.
6. Commit.

This order ensures a rotation concurrent with setup/reset either completes first and is included in revoke-all, or waits and observes revoked/ineligible state. Non-relational test providers exercise business branches but do not prove the MySQL lock.

## Migration decision

No migration and no model snapshot update.

- Add no password or administrator column/table.
- Add no account-type column to refresh tokens.
- Add no community relationship.
- Preserve every existing user, Google association, Identity login, refresh token, and administrator privilege.
- Treat any generated model diff as an unintended implementation change requiring investigation.
