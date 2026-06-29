# Data Model: Owner Student License Management

## StudentLicense

Represents one community-owned student seat assigned by email.

### Fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| Id | long | Yes | Primary key |
| CommunityId | long | Yes | Owning community |
| Email | string | Yes | Trimmed/lowercased normalized student email, max 256 |
| UserId | long? | No | Future linked user after activation |
| PlayerProfileId | long? | No | Future linked player profile after activation |
| GradeId | long | Yes | Assigned grade |
| ClassId | long | Yes | Assigned class |
| Status | StudentLicenseStatus | Yes | Pending, Active, Revoked |
| EmailChangeCount | int | Yes | Starts at 0 |
| AssignedByUserId | long | Yes | Owner who assigned the license |
| ActivatedAt | DateTime? | No | Set by future activation feature only |
| CreatedAt | DateTime | Yes | Creation timestamp |
| UpdatedAt | DateTime? | No | Last update timestamp |

### Relationships

- `Community 1 -> 0..* StudentLicenses`
- `Grade 1 -> 0..* StudentLicenses`
- `Class 1 -> 0..* StudentLicenses`
- `User 1 -> 0..* assigned StudentLicenses` through `AssignedByUserId`
- `User 1 -> 0..* activated StudentLicenses` through nullable `UserId`
- `Player 1 -> 0..* StudentLicenses` through nullable `PlayerProfileId`

### Indexes

- `StudentLicenses.CommunityId`
- `StudentLicenses.CommunityId + Email + Status` for duplicate and search support
- `StudentLicenses.CommunityId + Status`
- `StudentLicenses.CommunityId + GradeId`
- `StudentLicenses.CommunityId + ClassId`
- Optional nullable indexes on `UserId` and `PlayerProfileId`

### Validation Rules

- `CommunityId`, `GradeId`, `ClassId`, and `AssignedByUserId` must be positive.
- `Email` must be valid, trimmed, lowercased, and at most 256 characters.
- `Status` defaults to Pending.
- `EmailChangeCount` defaults to 0 and cannot be negative.
- `GradeId` must belong to the route community.
- `ClassId` must belong to the route community.
- `ClassId` must belong to `GradeId`.
- Class must be Active, not deleted.
- Duplicate non-revoked email within the same community is rejected.

### State Transitions

```text
Pending -> Active   (future activation feature, not this feature)
Pending -> Revoked  (owner revocation)
Active  -> Revoked  (owner revocation)
Revoked -> Revoked  (idempotent no-op)
```

This feature does not implement `Pending -> Active`.

## StudentLicenseStatus

| Value | Meaning | Counts toward UsedStudents |
|---|---|---:|
| Pending | Seat reserved, no student access yet | Yes |
| Active | Seat activated and linked to student access | Yes |
| Revoked | Seat released, retained for history | No |

## CommunityLicense

Existing capacity record.

Relevant fields:

- `MaxStudents`
- `UsedStudents`
- `StudentEmailChangeLimit`

Rules:

- Add license: requires `UsedStudents < MaxStudents`, then increments `UsedStudents`.
- Update license email/assignment: never changes `UsedStudents`.
- Revoke Pending or Active license: decrements `UsedStudents` once, never below zero.
- Revoke already Revoked license: does not change `UsedStudents`.

## Grade

Existing community-owned grade.

Rules:

- Must belong to route community.
- Used as the selected grade on student licenses.

## Class

Existing community-owned class.

Rules:

- Must belong to route community.
- Must belong to selected grade.
- Must be Active for new or updated license assignment.

## CommunityUser

Existing membership/access record.

Rules:

- Requesting user must have Active Owner membership for the route community.
- Pending student licenses do not create CommunityUser rows.
- Revoking an Active student license with `UserId` removes or marks Removed only the matching `CommunityUser` row where `CommunityId`, `UserId`, and `Role = Student` match.
- Teacher, Owner, and unrelated community memberships are not modified.

## User

Existing login identity.

Rules:

- `AssignedByUserId` references the Owner assigning the license.
- Nullable `UserId` supports future student activation.
- This feature does not create users for pending student licenses.
