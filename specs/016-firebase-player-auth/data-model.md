# Data Model: Firebase Player Authentication

## Overview

This feature adds no table. It extends the existing shared user identity with a Firebase UID and relaxes the legacy player's Google-only requirement. Internal `User.Id` remains the authoritative link between authentication, player ownership, memberships, licenses, and profile updates.

## User

Existing table/entity: `Users` / `Infrastructure.DataAccess.User`.

### New Field

| Field | Type | Null | Constraints | Purpose |
|---|---|---:|---|---|
| FirebaseUid | string, max 128 | Yes | Unique when present | Firebase project user identifier, stored separately from GoogleId |

### Existing Fields Used

| Field | Role in this feature |
|---|---|
| Id | Internal authoritative user identity and JWT `userId` |
| GoogleId | Existing Google provider identity; never stores Firebase UID |
| NormalizedEmail | Canonical email lookup used only when Firebase verifies the email |
| Email | Required response and activation identity |
| Name | Login display name; email fallback when Firebase name is absent |
| AvatarUrl | Optional response picture |
| Status | Suspended users cannot log in |
| Player | Existing one-to-one navigation by Player.UserId |

### Validation and Invariants

- FirebaseUid is nullable so every existing Google/password user migrates safely.
- A non-null FirebaseUid belongs to exactly one User.
- FirebaseUid and GoogleId represent different identity namespaces and are never copied into one another.
- A selected user's existing non-null different FirebaseUid or conflicting GoogleId is never overwritten.
- NormalizedEmail may link an existing user only when the Firebase token says the email is verified.
- An unverified email collision returns a conflict; it does not link and does not create a duplicate email.
- Suspended status is checked after resolution and before player mutation, activation, or token issuance.

## Player Profile

Existing table/entity: `Players` / `Domain.Models.Player`.

### Changed Field

| Field | Type | Null before | Null after | Constraints |
|---|---|---:|---:|---|
| GoogleId | string, max 128 | No | Yes | Unique when present |

### Existing Fields Used

| Field | Role in this feature |
|---|---|
| Id | Returned as PlayerProfileId and included in JWT |
| UserId | Primary player lookup and authoritative one-to-one user ownership |
| Email | Verified-email fallback for safe attachment of a legacy unlinked player |
| Name / AvatarUrl | Identity display fields set when creating or safely attaching |
| Gold / Experience / Level | Returned unchanged in LoginResponse |
| Rp / RankTier / HighestRankTier / TotalMatches / TotalWins | Existing progression defaults preserved for new players |
| Age / Grade / SchoolName | Existing editable profile fields preserved on login/linking |

### Validation and Invariants

- UserId remains unique: one player per user.
- GoogleId remains unique when present; MySQL permits multiple null values in the unique index.
- Firebase-only players have `GoogleId = null`.
- FirebaseUid is never stored on Player.
- Player resolution order is UserId, verified Google provider ID, then verified normalized email.
- A player linked to another UserId is never reassigned.
- Repeat login never resets progression or editable profile data.

### New Player Defaults

| Field | Default |
|---|---:|
| Gold | 0 |
| Experience | 0 |
| Level | 1 |
| Rp | 0 |
| RankTier | Student |
| HighestRankTier | Student |
| TotalMatches | 0 |
| TotalWins | 0 |

## Firebase User Response

Transient SDK-neutral Shared response; not persisted.

| Field | Required | Source |
|---|---:|---|
| Uid | Yes | Verified Firebase token UID |
| Email | Yes | Verified token email claim |
| EmailVerified | Yes | Verified token email-verification claim |
| Name | No | Verified token name/display claim |
| PictureUrl | No | Verified token picture claim |
| SignInProvider | No | Verified nested Firebase provider claim |
| GoogleProviderId | No | Verified nested `google.com` identity |

No raw ID token, Firebase SDK object, credential, or full claim dictionary leaves Infrastructure.

## External Player Login Context

Transient Application object; not persisted.

| Field | Purpose |
|---|---|
| Subject | Provider subject placed in JWT `sub` for compatibility |
| GoogleProviderId | Safe legacy Google player lookup/population when present |
| Email | Response, player creation/attachment, and activation matching |
| EmailVerified | Gates email-based linking and teacher activation |
| Name | Response/player display name |
| PictureUrl | Response/player avatar |
| ActivatePendingTeachers | Provider policy; true for Firebase, false for Google |

## Existing Community Entities

### StudentLicense

No schema change.

- Existing Pending licenses match verified normalized email.
- Activation sets UserId, PlayerProfileId, Active status, ActivatedAt, and UpdatedAt.
- UsedStudents is unchanged because the pending license already reserved the seat.
- Revoked/Active licenses are not reprocessed.

### CommunityUser

No schema change.

- Student activation creates/restores Active Student membership under existing rules.
- Firebase teacher activation considers only Pending Teacher memberships owned by the resolved UserId.
- Nonmatching roles, communities, and users are untouched.
- `UsedTeachers` is unchanged during login activation.

### TeacherInvitation

No schema change.

- A matching usable invitation must be unexpired, unrevoked, unaccepted, and match the verified normalized email.
- Firebase login activation sets the membership Active and `AcceptedAt` on the usable invitation in one save.
- Expired, revoked, superseded, or mismatched invitation state does not activate.
- A legacy pending Teacher membership with no invitation record may activate for its already-linked user and verified email.

## Identity Resolution State Machine

```text
Verified Firebase response
        |
        v
Collect candidates by FirebaseUid, GoogleId, verified NormalizedEmail
        |
        +-- candidates disagree / duplicate key rows --> HTTP 409, no mutation
        |
        +-- FirebaseUid match ------------------------> selected User
        |
        +-- GoogleId match ---------------------------> selected User
        |
        +-- verified email match ---------------------> selected User
        |
        `-- no match + no email collision ------------> create User
                                                         |
                                                         v
                                      attach missing nonconflicting identities
```

After selection, every available trusted identity is checked again against the selected user before mutation.

## Login State Transitions

### First Firebase Login

```text
No User -> Active User(FirebaseUid)
No Player -> Player(UserId, GoogleId null or verified Google provider ID)
Eligible Pending licenses/memberships -> existing activation transitions
No eligible business records -> B2C login with no community state
```

### Repeated Firebase Login

```text
User(FirebaseUid) -> same User
Player(UserId) -> same Player with progression unchanged
Already active records -> unchanged
SprintLabs access token -> newly issued
```

### Identity Conflict

```text
Trusted identifiers resolve different records
    -> HTTP 409
    -> no FirebaseUid/GoogleId update
    -> no User/Player creation
    -> no membership/license mutation
    -> no SprintLabs token
```

## Migration Design

Migration name: `AddFirebasePlayerAuthentication`.

### Up

1. Add nullable `Users.FirebaseUid` with max length 128.
2. Add a unique index on `Users.FirebaseUid`.
3. Alter `Players.GoogleId` from required to nullable, preserving max length and unique index.
4. Update the model snapshot.

No backfill is required. Existing Google user/player values remain unchanged.

### Down

1. Drop the FirebaseUid index and column.
2. Restore required Player.GoogleId only after handling Firebase-only null rows deterministically.

Generated Down code must be inspected. It must not delete players or copy Firebase UID into GoogleId. If rollback support requires placeholders for Firebase-only rows, use non-sensitive deterministic legacy placeholders documented for rollback, or require a pre-rollback repair step.

## Concurrency and Integrity

- Unique `Users.FirebaseUid` prevents duplicate Firebase users across service instances.
- Existing unique `Users.NormalizedEmail` prevents duplicate email identities.
- Existing unique `Players.UserId` prevents duplicate player profiles.
- Existing unique `Players.GoogleId` prevents duplicate legacy Google player ownership.
- Application preflight provides friendly conflicts; database constraints are the final concurrency guard.
- A uniqueness failure is followed by authoritative re-read. A consistent winner may be returned; disagreement returns HTTP 409.
