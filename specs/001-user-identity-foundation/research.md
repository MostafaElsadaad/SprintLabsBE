# Research: User Identity Foundation

## Decision: Extend the existing Identity user entity

**Decision**: Extend `Infrastructure.DataAccess.User : IdentityUser<long>` with Sprint Labs identity fields: GoogleId, Name, AvatarUrl, IsPlatformAdmin, Status, CreatedAt, and UpdatedAt.

**Rationale**: The project already configures `AddIdentityCore<User>()`, `ApplicationDbContext : IdentityDbContext<User, IdentityRole<long>, long>`, and table-name normalization from `AspNetUsers` to `Users`. Extending the existing entity preserves the current auth infrastructure and avoids creating two competing user stores.

**Alternatives considered**:

- Add a separate `Domain.Models.User` table: rejected because it would split login identity from the existing Identity user table and require extra synchronization.
- Introduce a new auth system: rejected by feature constraints and unnecessary because IdentityCore already exists.

## Decision: Keep Player as PlayerProfile

**Decision**: Use the current `Domain.Models.Player` and `Players` table as the player profile entity.

**Rationale**: Existing Google login, update profile, responses, repositories, and migrations already treat `Player` as the game profile/progression record. Reusing it preserves game behavior and minimizes changes.

**Alternatives considered**:

- Add a new `PlayerProfile` table: rejected because it duplicates current game profile data and would force unnecessary migration complexity.
- Rename `Player` now: rejected because it creates broad churn without changing behavior.

## Decision: Keep Player.UserId nullable in the first migration

**Decision**: Add `Player.UserId` as nullable in the first migration and configure the relation as User 1 -> 0..1 Player.

**Rationale**: Existing player records may not have safe user matches immediately. Nullable rollout avoids blocking migration and prevents unsafe forced links.

**Alternatives considered**:

- Required `Player.UserId`: rejected because existing player rows could fail migration or require unsafe backfill assumptions.
- Separate staging table for links: rejected as too heavy for this slice.

## Decision: Match Google login users by email

**Decision**: Google login finds the user by normalized email first, creates one if absent, and stores/updates GoogleId when available.

**Rationale**: The spec explicitly requires create/find by Google email. Email matching also handles existing accounts created before GoogleId is stored on User.

**Alternatives considered**:

- Match only by GoogleId: rejected because existing users may not have GoogleId yet and the acceptance criteria require email matching.
- Match by Player.GoogleId first: rejected as the primary identity rule, though it remains useful for player-profile backward compatibility.

## Decision: JWT includes UserId and player identifier when available

**Decision**: Add a user id claim and keep an existing player identifier/Google identity claim where needed for backward compatibility.

**Rationale**: New `/api/users/me` endpoints need reliable current-user resolution by UserId, while existing player/profile flows currently depend on GoogleId-based player lookup.

**Alternatives considered**:

- Use only Google subject claim: rejected because current-user endpoints should resolve platform identity even if GoogleId is missing or updated later.
- Use only player id claim: rejected because future non-player users may have no player profile.

## Decision: Use MySQL-safe nullable unique index for Player.UserId

**Decision**: Configure a unique index on `Player.UserId` while nullable, and verify generated MySQL migration behavior.

**Rationale**: The one-to-zero-or-one relation requires uniqueness for non-null links. MySQL permits multiple null values in unique indexes, which fits the rollout requirement.

**Alternatives considered**:

- Filtered unique index: EF Core filtered indexes are not portable to MySQL in the same way as SQL Server; avoid provider-specific unsupported filtering unless generated migration proves support.
- No unique index: rejected because it would allow multiple player profiles linked to one user.

## Decision: Add current-user CQRS features under Application/Features/Users

**Decision**: Add `GetCurrentUser` and `GetCurrentPlayerProfile` query slices under `Application/Features/Users`.

**Rationale**: New endpoints are user-facing identity reads and should not be tucked into the existing account login/update feature. This keeps controllers thin and follows the project CQRS pattern.

**Alternatives considered**:

- Put reads under `Application/Features/Accounts`: acceptable but less clear because these are not account mutation/login actions.
- Query repositories directly from controllers: rejected by existing MediatR conventions.
