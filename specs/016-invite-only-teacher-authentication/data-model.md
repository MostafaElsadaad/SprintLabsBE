# Data Model: Invite-Only Teacher Authentication

## Model impact summary

No new domain table is required. The feature reuses `User`, `CommunityUser`, `TeacherInvitation`, `Community`, `CommunityLicense`, and `RefreshToken`. One nullable user timestamp and lookup indexes are added through a single non-destructive migration.

## User

Existing relevant fields include Identity identifiers and normalized values, `Name`, `EmailConfirmed`, password/lockout state, teacher account state, suspension/account status, and confirmation-email cooldown data.

### Added field

| Field | Type | Nullable | Purpose |
|---|---|---:|---|
| `LastPasswordResetEmailSentAt` | UTC `DateTime?` | Yes | Persisted resend/cooldown claim for enumeration-safe password-reset email delivery. Existing rows remain null. |

### Invariants

- Normalized email remains unique under the existing index; an invitation never creates a duplicate normalized email.
- Invited email is immutable through validation/completion; completion does not accept an email field.
- `IsTeacherAccount` is true before a pending teacher reservation is created.
- A completed invited teacher has a trimmed non-empty `Name`, confirmed email, and a password set through Identity.
- If an invited user has no username, completion assigns the normalized/deterministic email username. A pre-existing deterministic username is not overwritten.
- No invitation/login/recovery operation creates `PlayerProfile` or `StudentLicense`.

## CommunityUser

`CommunityUser` remains the authoritative teacher/community relationship.

### Current relationship definition

For this feature, a teacher's **current** relationship is a `CommunityUser` with role Teacher and status Active or Pending. Removed/revoked/expired historical states do not authorize login and do not reserve a community unless the existing enum explicitly models them as Pending.

### Invariants

- A teacher can have at most one current relationship across all communities for new invitation/completion operations.
- Invitation creation produces or reuses exactly one Pending Teacher row for the inviting community.
- Completion changes that same row from Pending to Active; it never inserts a second row.
- Password login requires exactly one Active Teacher row, zero Pending Teacher rows, and an active linked community.
- Same-community reissue does not increment `CommunityLicense.UsedTeachers` again.
- Cross-community conflict never removes or moves an existing relationship.

### Added index

Non-unique composite lookup index: `(UserId, Role, Status, CommunityId)` unless an equivalent already exists. It supports relationship cardinality checks and the transaction's locking lookup without constraining non-teacher roles or preserved legacy rows.

## TeacherInvitation

Existing fields provide the invitation/membership link, SHA-256 token hash, expiry, created/sender data, revocation, and acceptance/use state. Reissue audit fields should be reused if present; add fields only if inspection during implementation proves required metadata is absent.

### Invariants

- The raw token is random and exists only in the outbound link/request. Only its SHA-256 hash is persisted.
- Token hash remains uniquely indexed.
- A usable invitation is unaccepted, unrevoked, unexpired, and linked to a Pending Teacher membership in an active community.
- Same-community resend revokes the preceding usable invitation before creating the replacement.
- Validation performs no write.
- Completion marks exactly one invitation accepted/used inside the same transaction that activates the membership.
- Invalid, expired, revoked, used, or replayed tokens share one safe public error.

### Added index

Non-unique composite lookup index: `(CommunityUserId, AcceptedAt, RevokedAt, ExpiresAt)` unless an equivalent exists. The existing unique token-hash index remains unchanged.

## Community and CommunityLicense

- Only an Active community can issue, validate, complete, or authorize password login.
- The caller must have an Active Owner `CommunityUser` for the route community; this is rechecked under the invitation transaction.
- `CommunityLicense` is locked before a new Pending Teacher seat is reserved.
- `UsedTeachers` increments once when a new/restored reservation is made and not on resend, completion, or login.

## RefreshToken

No schema change is planned. Existing hashed token, expiry, revocation, rotation, and user linkage remain authoritative.

- Login calls the existing token issuance after membership eligibility.
- Successful password reset revokes every active refresh token for the user.
- Invitation completion revokes active refresh tokens only if it replaces an existing password.
- Validation and normal completion do not issue access or refresh tokens.

## State transitions

```text
No current teacher relationship
  -- owner invite --> Pending CommunityUser + usable TeacherInvitation

Pending CommunityUser + usable invitation
  -- same community resend --> same Pending CommunityUser + old invitation revoked + replacement invitation
  -- different community invite --> no change + HTTP 409
  -- validate --> no change
  -- complete --> Active CommunityUser + accepted invitation + completed teacher credentials

Active CommunityUser
  -- password login with eligible account --> token pair + singular community response
  -- different community invite --> no change + HTTP 409

Expired/revoked/accepted invitation
  -- validate or complete --> no change + safe invalid-invitation error
```

## Transaction/locking order

Use a consistent order to reduce deadlocks:

1. Caller owner membership, community, and (when reserving a new seat) community license.
2. Target user selected by normalized email.
3. Target user's Active/Pending Teacher relationships ordered by stable key.
4. Same-community membership.
5. Invitation rows ordered by stable key.

Completion begins by locking the invitation identified by token hash, then its membership and user, because the token is the only request identity. It subsequently locks/checks the user's other current relationships before mutation. Uniqueness/deadlock conflicts are retried/requeried once and translated to the deterministic final business outcome.

## Migration safety

- Add `LastPasswordResetEmailSentAt` as nullable with no backfill.
- Add only missing non-unique lookup indexes.
- Preserve existing unique normalized-email and invitation-token-hash indexes.
- Do not delete, merge, or rewrite users or relationships.
- Do not add a global `CommunityUser.UserId` unique constraint or a teacher-only generated constraint that could reject preserved legacy data or valid non-teacher relationships.
- Inspect the generated migration and model snapshot for unintended column/table/index changes before applying it.
