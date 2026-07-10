# Research: Progression Persistence Foundation

## Decision: Use `Player` as the PlayerProfile persistence entity

**Rationale**: The project currently stores player profile data in `Domain/Models/Player.cs` and exposes it as a player profile through existing responses and endpoints. Adding progression summary fields there preserves existing login/profile behavior and avoids a duplicate profile model.

**Alternatives considered**:

- Add a separate PlayerProfile table: rejected because the project already uses `Player` as the profile table and the feature requests existing PlayerProfile support.
- Add a separate progression summary table: rejected because the requested fields are player-profile summary fields and should be directly queryable for future leaderboard/ranking work.

## Decision: Preserve existing progression fields

**Rationale**: `Player` already has `Experience`, `Level`, and `Gold` with safe defaults. Implementation should retain those fields and only add missing fields: `Rp`, `RankTier`, `HighestRankTier`, `TotalMatches`, and `TotalWins`.

**Alternatives considered**:

- Recreate existing fields in new structures: rejected as unnecessary and risky for backward compatibility.
- Rename existing fields: rejected because existing API/login/profile responses already depend on these names.

## Decision: Add grouped progression enums

**Rationale**: The current enum folder uses grouped enum files such as `CommunityEnums.cs`. A new `ProgressionEnums.cs` keeps `MatchType`, `MatchStatus`, `RankTier`, and `XpSourceType` cohesive without mixing them into community-specific enums.

**Alternatives considered**:

- One enum file per enum: rejected because grouped enum files are already an accepted local convention.
- Put enum values as strings only: rejected because existing domain enums use typed enum values and EF stores them as integers by default.

## Decision: Add grouped progression entities in `Domain/Models`

**Rationale**: Current domain models live in `Domain/Models`, and related community entities are grouped in one file. A cohesive `Progression.cs` file can hold `Match`, `MatchPlayer`, `MatchQuestionResult`, `MatchRewardResult`, `PlayerXpLog`, and `PlayerRankLog` without adding unrelated architecture.

**Alternatives considered**:

- One entity file per model: valid, but more file churn than needed for this tightly related schema foundation.
- Feature-specific repository classes: rejected because no custom persistence behavior is required.

## Decision: Configure EF Core inline in `ApplicationDbContext`

**Rationale**: The current project configures entity properties, relationships, defaults, and indexes inline in `ApplicationDbContext.OnModelCreating`. Following that style avoids introducing separate configuration classes mid-feature.

**Alternatives considered**:

- New `IEntityTypeConfiguration<T>` classes: rejected because the current project has not adopted that pattern.
- Convention-only configuration: rejected because the feature requires specific defaults, indexes, unique constraints, nullable community relationships, and delete behavior choices.

## Decision: Use nullable `CommunityId` for B2C-capable records

**Rationale**: B2C matches and progression logs must exist without a community. B2B school records can carry a community id. Therefore `CommunityId` should be nullable on `Match`, `MatchPlayer`, `MatchRewardResult`, `PlayerXpLog`, and `PlayerRankLog`.

**Alternatives considered**:

- Require community context for all records: rejected because it breaks B2C.
- Store B2C records in separate tables: rejected because it duplicates match/progression concepts and complicates future queries.

## Decision: Avoid required CommunityUser or StudentLicense links

**Rationale**: The feature explicitly says persistence records must not require membership or license rows. Community ownership is represented only by optional `CommunityId`.

**Alternatives considered**:

- Link match records through CommunityUser: rejected because B2C users may have no membership.
- Link match records through StudentLicense: rejected because B2C players and some teachers/owners may have no student license.

## Decision: Use restrictive/no-action delete behavior for new cross-table links

**Rationale**: MySQL commonly runs into multiple cascade path or accidental data-loss issues when many result/log tables link to Player, Match, and Community. Existing code already uses `Restrict` for several non-owned references. New progression rows should preserve auditability and avoid cascade cycles.

**Alternatives considered**:

- Cascade delete all child rows: rejected because progression and audit logs should not disappear casually and could create MySQL cascade issues.
- Set null for all relationships: rejected where required parent links, such as match and player profile, must remain valid.

## Decision: Store `QuestionType` as a simple string/label

**Rationale**: The current Questions system stores question packs as JSON and does not expose a typed `QuestionType` enum/model in the domain. `MatchQuestionResult.QuestionType` should preserve the observed type without normalizing question internals in this feature.

**Alternatives considered**:

- Add a new question-type enum now: rejected because question type semantics are outside this persistence foundation and current questions are JSON-based.
- Add a foreign key to Questions for every result: rejected because the spec allows nullable `QuestionId`.

## Decision: Generate a real EF Core migration

**Rationale**: The feature adds tables and fields. A migration named `AddProgressionPersistenceFoundation` is required and should be reviewed for expected tables, nullable `CommunityId`, defaults, unique indexes, and snapshot changes.

**Alternatives considered**:

- Defer migration: rejected because persistence foundation acceptance criteria require it.
- Hand-write SQL: rejected because existing project uses EF Core migrations.
