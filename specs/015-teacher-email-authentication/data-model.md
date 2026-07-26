# Data Model: Teacher Email Authentication

## Overview

This feature extends the existing `Users` identity table, keeps `CommunityUsers.CommunityId` required, and adds `RefreshTokens` and `TeacherInvitations`. It does not add a Teacher profile, Player profile, Student license, global Teacher role, or nullable community membership.

## User

Implemented by the existing Infrastructure Identity `User`.

### New and Restored Fields

| Field | Type | Null | Default | Purpose |
|---|---|---:|---|---|
| IsTeacherAccount | boolean | No | false | Marks eligibility for teacher authentication; never grants community access |
| LastConfirmationEmailSentAt | UTC datetime | Yes | null | Enforces confirmation resend cooldown |
| EmailConfirmed | boolean | No | false | Identity email-verification state; currently ignored |
| LockoutEnd | UTC datetime offset | Yes | null | End of active Identity lockout; currently ignored |
| AccessFailedCount | integer | No | 0 | Failed password attempts; currently ignored |
| LockoutEnabled | boolean | No | true | Enables lockout for password accounts; currently ignored |

Existing `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `NormalizedEmail`, `GoogleId`, `Status`, `Name`, and email fields remain in use.

### Validation and Indexes

- `NormalizedEmail` is required for registered teacher accounts and must be unique.
- Existing unique email protection remains.
- `Name` remains required with maximum length 100.
- `IsTeacherAccount` defaults false for non-teacher identities.
- Migration backfills `IsTeacherAccount=true` for users with any existing Teacher membership.
- Existing Google/player identities with `IsTeacherAccount=false` cannot gain a password through public teacher registration.

### Account States

```text
No identity
  -> register
Teacher / password set / email unconfirmed / zero memberships

Passwordless invited identity
  -> register with matching normalized email
Same identity / teacher marker set / password set / email unconfirmed

Unconfirmed teacher
  -> valid confirmation
Confirmed teacher

Active confirmed teacher
  -> five failed password attempts
Temporarily locked teacher

Active teacher
  -> suspension
Suspended teacher (login and refresh denied)
```

Email confirmation changes account state only; it never changes `CommunityUser`.

## RefreshToken

New Domain entity and `RefreshTokens` table.

| Field | Type | Null | Validation / Index |
|---|---|---:|---|
| Id | long | No | Primary key, generated |
| UserId | long | No | FK to `Users.Id`; indexed |
| TokenHash | char(64) ASCII | No | Unique SHA-256 hex digest |
| ExpiresAt | UTC datetime | No | Indexed with user/revocation for active-session queries |
| CreatedAt | UTC datetime | No | Set on issue |
| RevokedAt | UTC datetime | Yes | Null means not revoked |
| ReplacedByTokenHash | char(64) ASCII | Yes | Hash of rotated replacement; not a bearer token |
| CreatedByIp | varchar(64) | Yes | IPv4/IPv6 textual address when available |
| RevokedByIp | varchar(64) | Yes | IPv4/IPv6 textual address when available |

### Relationships

- `RefreshToken.UserId -> Users.Id`.
- Delete behavior: cascade with user deletion so no orphaned credentials remain.
- One User can have many refresh-token history rows.

### Derived State

| State | Condition |
|---|---|
| Active | `RevokedAt is null` and `ExpiresAt > now` |
| Expired | `ExpiresAt <= now` |
| Revoked | `RevokedAt is not null` |
| Rotated | Revoked and `ReplacedByTokenHash is not null` |

### State Transitions

```text
Issued -> successful refresh -> Revoked/Rotated + new Issued token
Issued -> logout             -> Revoked
Issued -> password reset     -> Revoked
Issued -> time passes        -> Expired
Revoked/Expired -> refresh   -> rejected
```

Rotation is atomic: the old row is conditionally revoked and the replacement inserted in one transaction. Raw values exist only when returned to the client and are never persisted.

## TeacherInvitation

New Domain entity and `TeacherInvitations` table.

| Field | Type | Null | Validation / Index |
|---|---|---:|---|
| Id | long | No | Primary key, generated |
| CommunityUserId | long | No | FK to pending Teacher membership; indexed |
| InvitedEmail | varchar(256) | No | Stored normalized |
| TokenHash | char(64) ASCII | No | Unique SHA-256 hex digest |
| ExpiresAt | UTC datetime | No | Seven days by default |
| AcceptedAt | UTC datetime | Yes | Set once on acceptance |
| RevokedAt | UTC datetime | Yes | Set on reissue or membership removal |
| CreatedAt | UTC datetime | No | Set on issue |
| CreatedByUserId | long | No | FK to inviting Owner user |
| LastSentAt | UTC datetime | Yes | Set for initial issue and reissue record |

### Relationships

- `TeacherInvitation.CommunityUserId -> CommunityUsers.Id`, cascade on membership deletion.
- `TeacherInvitation.CreatedByUserId -> Users.Id`, restrict on user deletion.
- A CommunityUser can have invitation history; at most one unaccepted, unrevoked, unexpired invitation is treated as active by the service.
- Each invitation belongs indirectly to exactly one required community through `CommunityUser.CommunityId`.

### Invitation State

| State | Condition |
|---|---|
| Active | `AcceptedAt is null`, `RevokedAt is null`, and `ExpiresAt > now` |
| Accepted | `AcceptedAt is not null` |
| Revoked | `RevokedAt is not null` |
| Expired | Unaccepted/unrevoked and `ExpiresAt <= now` |

### State Transitions

```text
New pending membership -> Active invitation
Active invitation -> reissue -> Revoked + new Active invitation
Active invitation -> matching confirmed teacher accepts -> Accepted
Active invitation -> teacher removed -> Revoked
Active invitation -> seven days pass -> Expired
Accepted invitation -> same matching teacher repeats -> return existing Active membership
```

## CommunityUser

Existing entity; no new columns and `CommunityId` remains non-nullable.

| Field | Relevant rule |
|---|---|
| CommunityId | Required target community |
| UserId | Invited/accepted teacher identity |
| Role | Must be `Teacher` for this workflow |
| Status | `Pending`, `Active`, or `Removed` |
| CreatedAt / UpdatedAt | Existing audit timestamps |

### Membership Transitions

```text
No membership + invite with capacity -> Pending
Removed + re-invite with capacity     -> Pending
Pending + explicit valid acceptance  -> Active
Pending or Active + owner removal    -> Removed
```

- Registration and email confirmation never create or activate a membership.
- Google/password login never activates a pending Teacher membership.
- Only Active memberships authorize community access and appear in login/current-community results.
- The unique `(CommunityId, UserId)` index continues to prevent duplicates.

## CommunityLicense

Existing entity; no new columns.

### Seat Accounting

| Operation | `UsedTeachers` change |
|---|---:|
| Create new Pending Teacher membership | +1 |
| Restore Removed membership to Pending | +1 |
| Reissue Pending invitation | 0 |
| Invite existing Active membership | 0 |
| Accept Pending invitation | 0 |
| Remove Pending membership | -1 |
| Remove Active membership | -1 |
| Remove already Removed membership | 0 |

Capacity reservation and membership/invitation changes are committed in one transaction. The license row is locked or conditionally updated so concurrent invites cannot exceed `MaxTeachers`.

## Database Migration

Migration suffix: `TeacherEmailAuthentication`.

### Up

1. Add the six User fields above.
2. Make the Identity normalized-email index unique after verifying no duplicates.
3. Backfill `IsTeacherAccount=true` for users with any Teacher membership.
4. Create `RefreshTokens` with its foreign key and indexes.
5. Create `TeacherInvitations` with membership/inviter foreign keys and indexes.
6. Preserve `CommunityUsers.CommunityId` as required and preserve all existing active memberships.

### Down

1. Drop `TeacherInvitations`.
2. Drop `RefreshTokens`.
3. Restore the normalized-email index to its prior non-unique definition.
4. Drop the new User columns.

Down migration does not alter `CommunityUsers`, community licenses, players, or student licenses.

## Transaction Boundaries

| Use case | Transaction boundary |
|---|---|
| Register new/reused teacher | Identity creation/password/name/marker/confirmation timestamp commit together; email is sent after commit |
| Resend confirmation | Conditional cooldown timestamp update claims the send window; email follows |
| Login | Password/lockout update uses Identity persistence; refresh-token insert completes before response |
| Refresh | Conditional old-token revoke plus replacement insert in one transaction |
| Logout | Idempotent conditional revoke in one database operation |
| Reset password | Password/security-stamp update plus revoke-all active refresh tokens in one database transaction |
| Invite/reissue | User marker, membership, seat, old invitation revocation, and new invitation commit in one transaction; email follows |
| Accept invitation | Invitation validation, membership activation, and `AcceptedAt` update commit in one transaction |
| Remove teacher | Membership removal, one seat decrement, and active invitation revocation commit together |

