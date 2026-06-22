# Data Model: User Identity Foundation

## User

**Source**: Extend existing `Infrastructure.DataAccess.User : IdentityUser<long>`.

**Purpose**: Shared login identity for Sprint Labs users, including B2C players now and future B2B identities later.

### Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | long | Yes | Existing identity primary key |
| GoogleId | string? | No | Google subject when available |
| Email | string | Yes | Existing Identity email; unique |
| UserName | string | Yes | Keep aligned with email unless existing conventions require otherwise |
| Name | string | Yes | Display name from Google login |
| AvatarUrl | string? | No | Google picture URL |
| IsPlatformAdmin | bool | Yes | Defaults to false |
| Status | UserStatus | Yes | Defaults to Active |
| CreatedAt | DateTime | Yes | UTC creation timestamp |
| UpdatedAt | DateTime? | No | UTC update timestamp |

### Relationships

- One User has zero or one Player.
- User may exist without Player for future non-player platform identities.

### Indexes and Constraints

- `Users.Email` unique.
- `Users.GoogleId` non-unique optional index unless implementation confirms GoogleId must be unique across all records.
- Required max lengths should follow existing Identity and Player conventions where available.

### State

| Status | Meaning |
|--------|---------|
| Active | User can authenticate and use authorized identity endpoints |
| Suspended | User is blocked from authenticated identity actions with controlled errors |

## UserStatus

**Source**: New enum in `Domain/Enums/UserStatus.cs`.

Values:

- `Active`
- `Suspended`

## Player

**Source**: Existing `Domain.Models.Player`.

**Purpose**: Current game profile/progression record. This is the PlayerProfile for this feature.

### Existing Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | long | Yes | Existing player primary key |
| GoogleId | string | Yes | Existing player Google identifier |
| Email | string | Yes | Existing player email |
| Name | string | Yes | Existing player display name |
| AvatarUrl | string? | No | Existing player avatar |
| Age | int? | No | Existing profile field |
| Grade | int? | No | Existing profile field |
| SchoolName | string? | No | Existing profile field |
| Gold | int | Yes | Existing progression; default 0 |
| Experience | int | Yes | Existing progression; default 0 |
| Level | int | Yes | Existing progression; default 1 |
| CreatedAt | DateTime | Yes | Existing timestamp |
| UpdatedAt | DateTime? | No | Existing timestamp |

### New Field

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| UserId | long? | No | Nullable in first migration for safe rollout/backfill |

### Relationships

- Player belongs to zero or one User during rollout.
- Player must not be linked to more than one User.
- User must not be linked to more than one Player.

### Indexes and Constraints

- Existing `Players.Email` unique remains.
- Existing `Players.GoogleId` unique remains.
- New `Players.UserId` unique nullable index.
- Foreign key from `Players.UserId` to `Users.Id` with null allowed.

## Login Response

**Source**: Extend existing `Shared.Responses.LoginResponse`.

### Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| AccessToken | string | Yes | Existing JWT |
| UserId | long | Yes | New shared identity id |
| PlayerProfileId | long? | No | Player id when player profile exists |
| Name | string | Yes | Existing response field |
| Email | string | Yes | Existing response field |
| PictureUrl | string? | No | Existing response field |
| Gold | int | Yes | Existing player progression |
| Experience | int | Yes | Existing player progression |
| Level | int | Yes | Existing player progression |

## CurrentUserResponse

**Source**: New DTO under `Application/Features/Users/GetCurrentUser` or shared response if reuse becomes necessary.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| UserId | long | Yes | Authenticated user id |
| Email | string | Yes | User email |
| Name | string | Yes | User display name |
| AvatarUrl | string? | No | User avatar |
| Status | UserStatus | Yes | Active or Suspended |
| IsPlatformAdmin | bool | Yes | Platform admin flag |
| PlayerProfileId | long? | No | Linked player id when present |

## Migration and Backfill Rules

- First migration creates/adds the User identity fields and nullable `Players.UserId`.
- Backfill links existing players to users only when the match is safe and unambiguous.
- Ambiguous duplicate email situations must not be auto-linked.
- Future migration may make `Players.UserId` required only after product and data checks prove every player profile has a safe user link.
