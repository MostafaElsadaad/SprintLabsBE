# Data Model: Production-Like Development Player Authentication

## Overview

This feature adds no persistent entity and no migration. A development player is a deterministic catalog definition backed by one existing Identity `User` and one existing `Player`. The database continues to generate and own both `long` identifiers.

## Development Player Definition

Transient immutable catalog item; not a table.

| Field | Type | Rules | Purpose |
|---|---|---|---|
| AccountKey | string | Exact ordinal value `dev-player-01` through `dev-player-08` | Public development selector and Identity username |
| DisplayName | string | `Dev Player 01` through `Dev Player 08` | Deterministic User/Player display value |
| Email | string | `dev-player-NN@development.sprintlabs.invalid` | Stable lookup through existing normalized email behavior |

The catalog is the single definition consumed by seeding, discovery, and login. Keys are not trimmed or case-normalized at the API boundary.

## Existing User Entity

Existing entity/table: `Infrastructure.DataAccess.User` / `Users`.

| Existing field | Development-player use |
|---|---|
| Id (`long`) | Authoritative UserId; database generated and stable across seed runs |
| UserName / NormalizedUserName | Canonical account key and Identity-normalized uniqueness |
| Email / NormalizedEmail | Deterministic stable lookup and uniqueness |
| Name | Canonical display name |
| Status | Must be Active; Suspended rejects login |
| LockoutEnd | A current lockout rejects login |
| IsPlatformAdmin | Must remain false for a seed-owned development player |
| IsTeacherAccount | Must remain false for a seed-owned development player |
| GoogleId / FirebaseUid | Must be null; development seeding never takes over an external identity |
| Player | Existing one-to-one relationship reached through `Player.UserId` |

Seeded users are passwordless, like provider-created player identities. The development API key is never stored on User and is not a player credential.

## Existing Player Entity

Existing entity/table: `Domain.Models.Player` / `Players`.

| Existing field | Development-player use |
|---|---|
| Id (`long`) | Authoritative PlayerProfileId; database generated and stable |
| UserId (`long?`) | Must equal the catalog User.Id; unique one-to-one ownership |
| Email | Matches the deterministic catalog email |
| Name | Canonical display name |
| AvatarUrl | Optional/empty unless seed policy later explicitly supplies a non-secret asset |
| Gold | Defaults to 0 on first creation; preserved on rerun |
| Experience | Defaults to 0 on first creation; preserved on rerun |
| Level | Defaults to 1 on first creation; preserved on rerun |
| Rp | Defaults to 0 on first creation; preserved on rerun |
| RankTier / HighestRankTier | Default Student; preserved on rerun |
| TotalMatches / TotalWins | Default 0; preserved on rerun |

No XP, RP, rank, match, mission, or telemetry service is called by this feature.

## Existing LoginResponse

Transient existing response; unchanged.

| Field | Source |
|---|---|
| AccessToken | Existing `IUserService.Authenticate` player JWT issuer |
| UserId | Persisted User.Id |
| PlayerProfileId | Persisted Player.Id |
| Name | Persisted/catalog display name through shared workflow mapping |
| Email | Persisted deterministic email |
| PictureUrl | Persisted User/Player avatar value, normally empty for initial seed |
| Gold / Experience / Level | Persisted Player values |

The JWT retains existing `email`, `sub`, `name`, `userId`, and `playerProfileId` claims. No development-only claim is added.

## Existing Constraints Reused

- Identity normalized username uniqueness prevents duplicate account keys.
- User email/normalized-email uniqueness prevents duplicate deterministic account identities.
- Player email uniqueness prevents duplicate deterministic profiles.
- Player UserId uniqueness prevents multiple profiles for one account.
- User-to-Player one-to-one mapping supplies the trusted PlayerProfileId returned by `/Users/me`.

No new constraint or migration is planned.

## Valid Seeded Pair Invariants

A catalog item is usable only when all conditions hold:

1. exactly one User matches its deterministic username/email;
2. the User has the catalog name, is Active, is not currently locked, and has no provider/privileged ownership conflict;
3. exactly one Player exists for User.Id;
4. the Player's UserId equals User.Id and its deterministic email/key identity does not point elsewhere;
5. generated User.Id and Player.Id are positive `long` values;
6. no other catalog item resolves either identifier.

Discovery validates all eight and returns no partial list. Login validates the selected pair only. Neither endpoint changes state.

## Seeder State Transitions

```text
Catalog item
  |
  +-- no User, no Player collision
  |     -> UserManager creates real User
  |     -> DbContext creates linked Player with defaults
  |
  +-- compatible User + Player
  |     -> preserve IDs and progression
  |     -> reconcile catalog-owned display/identity values if stale
  |
  +-- compatible User, missing Player
  |     -> seeder creates the missing linked Player
  |
  +-- partial interrupted set
  |     -> ensure remaining catalog items in the next enabled run
  |
  `-- provider/privilege/cross-link/duplicate ambiguity
        -> fail safely; do not take over, relink, or delete
```

For relational storage, the eight-item run is one serializable transaction. Existing unique constraints are the cross-process race backstop. A competing process may fail and retry at a later startup, but a duplicate committed identity is not accepted.

## Request-Time State Transitions

### Discovery

```text
guard -> validate all eight persisted pairs -> return safe catalog projections
```

No creation, update, activation, token issuance, or progression mutation occurs.

### Login

```text
guard -> exact catalog key -> load existing User -> load linked Player
      -> validate status/relationship -> shared existing-player JWT completion
```

A replay may issue a new JWT but changes no User/Player identity or progression.
