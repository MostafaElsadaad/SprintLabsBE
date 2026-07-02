# Data Model: Student License Activation on Login

## Existing Entities Used

### StudentLicense

Represents a student seat assigned to an email by a community Owner.

Relevant fields:

| Field | Type | Activation Behavior |
|-------|------|---------------------|
| Id | long | Identifies the license to activate |
| CommunityId | long | Determines which community receives Student access |
| Email | string | Matched to normalized Google login email |
| UserId | long? | Set to the resolved login user during activation |
| PlayerProfileId | long? | Set to the resolved or created player profile during activation |
| Status | StudentLicenseStatus | Changes from Pending to Active |
| ActivatedAt | DateTime? | Set when activation succeeds |
| UpdatedAt | DateTime? | Set when activation changes the license |

Status transitions for this feature:

```text
Pending -> Active
Active  -> Active   (ignored by login activation)
Revoked -> Revoked  (ignored by login activation)
```

Validation rules:

- Only `Pending` licenses are eligible.
- Matching is by normalized email.
- Activation links the license to both `UserId` and `PlayerProfileId`.
- Activation does not change grade, class, assigned-by user, email change count, or community license usage.

### User

Represents the shared login identity found or created by Google login.

Activation behavior:

- The resolved user id is assigned to every activated matching student license.
- Suspended users must keep existing login rejection behavior.

### Player

Represents the player profile used by game progress and profile behavior.

Activation behavior:

- Login must create or find the player profile before student license activation completes.
- The resolved player id is assigned to every activated matching student license.

### CommunityUser

Represents community access for a user.

Relevant fields:

| Field | Activation Behavior |
|-------|---------------------|
| CommunityId | Must match the activated license community |
| UserId | Must match the resolved login user |
| Role | Must be Student when creating/restoring student access |
| Status | Must become Active for created/restored Student access |
| UpdatedAt | Updated when restoring or changing membership status |

Activation behavior:

- If no membership exists for the license community and user, create Active Student membership.
- If Student membership exists with Removed or Pending status, restore it to Active.
- If Active Student membership already exists, leave it unchanged.
- If a non-Student membership exists for the same community and user, do not create a duplicate or overwrite the role.

### CommunityLicense

Represents community seat capacity.

Activation behavior:

- `UsedStudents` is not changed by this feature.
- Pending licenses already reserved student seats during owner license creation.

## Relationships

```text
User 1 -> 0..1 Player
Community 1 -> 0..* StudentLicense
Community 1 -> 0..* CommunityUser
User 1 -> 0..* CommunityUser
StudentLicense 0..1 -> 1 User
StudentLicense 0..1 -> 1 Player
```

## Persistence Notes

- No new table is required.
- No migration is required because `StudentLicense` already contains `UserId`, `PlayerProfileId`, `Status`, and `ActivatedAt`.
- Existing `CommunityUsers` uniqueness on `CommunityId + UserId` must be respected.
- Activation should save student license updates and membership updates together closely enough that login does not return success after only partial activation.
