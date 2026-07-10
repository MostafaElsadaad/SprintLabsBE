# Implementation Plan: Progression Persistence Foundation

**Branch**: `011-progression-persistence-foundation` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-progression-persistence-foundation/spec.md`

## Summary

Add the schema/domain persistence foundation for upcoming match completion and progression work. The implementation will extend the existing `Player`/PlayerProfile model with missing progression summary fields, add progression/match enums, add match/result/log entities, configure EF Core relationships/indexes/defaults in the existing DbContext style, and generate the `AddProgressionPersistenceFoundation` migration. No APIs, CQRS handlers, login behavior, authorization behavior, calculations, leaderboards, analytics, dashboards, or reports are included.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: EF Core 8, Pomelo MySQL provider, ASP.NET Core API startup project for migrations, existing Domain/Infrastructure project references

**Storage**: MySQL through EF Core migrations. Existing `Players` table already has `Experience`, `Level`, and `Gold`; this feature adds `Rp`, `RankTier`, `HighestRankTier`, `TotalMatches`, and `TotalWins`, plus new match/progression tables.

**Testing**: `dotnet build SprintLabs.sln`; focused persistence/model validation where useful; migration review for required tables, nullable `CommunityId`, default values, indexes, and unique constraints.

**Target Platform**: SprintLabs backend API persistence layer

**Project Type**: .NET backend API monolith with Domain, Infrastructure, Application, API, Shared, and test projects

**Performance Goals**: Persistence model should support future match completion, history, ranking, leaderboard, and progression-log queries through planned indexes without adding current runtime workflows.

**Constraints**: Persistence foundation only; follow existing entity, enum, DbContext, migration, EF Core, repository, and naming patterns; modify the fewest files; no APIs; no MediatR/CQRS unless unexpectedly required for persistence only; no XP/level/RP/rank calculations; no services; no leaderboards; no match history APIs; no Google login or community authorization changes.

**Scale/Scope**: One schema/domain slice: Player progression fields, four enums, six new persistence entities, DbContext configuration, migration, and validation docs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. This is intentionally a DB/domain persistence vertical slice with migration and verification; no API behavior is promised.
- **Existing architecture wins**: PASS. Uses existing `Domain/Models`, `Domain/Enums`, inline `ApplicationDbContext.OnModelCreating`, EF Core migrations, and current migration command pattern.
- **SaaS data isolation**: PASS. B2C records allow `CommunityId = null`; B2B records may carry `CommunityId`; records do not require `CommunityUser` or `StudentLicense`.
- **JSON where flexibility matters**: PASS. Existing questions remain JSON-backed; `MatchQuestionResult.QuestionType` stores the observed type value without normalizing question internals.
- **Minimum useful implementation**: PASS. No APIs, calculations, services, leaderboards, analytics, reports, dashboards, or game-server integration.
- **Quality gates**: PASS. Implementation must run `dotnet build SprintLabs.sln` and review the generated migration/snapshot.
- **Documentation is executable context**: PASS. Plan, research, data model, and quickstart live under `specs/011-progression-persistence-foundation/`.

## Project Structure

### Documentation (this feature)

```text
specs/011-progression-persistence-foundation/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md              # Generated later by /speckit-tasks
```

No `contracts/`, `api.md`, or `frontend.md` artifact is planned because this feature adds no external API and no frontend-facing behavior.

### Source Code (repository root)

```text
Domain/
|-- Enums/
|   `-- ProgressionEnums.cs
`-- Models/
    |-- Player.cs                  # Add missing summary fields
    `-- Progression.cs             # Match/result/log persistence entities

Infrastructure/
|-- DataAccess/
|   `-- ApplicationDbContext.cs     # DbSets, relationships, defaults, indexes
`-- Migrations/
    |-- *_AddProgressionPersistenceFoundation.cs
    |-- *_AddProgressionPersistenceFoundation.Designer.cs
    `-- ApplicationDbContextModelSnapshot.cs

SprintLabs.Tests/
`-- Features/
    `-- ProgressionPersistenceFoundation/
        `-- ProgressionPersistenceModelTests.cs
```

**Structure Decision**: Keep persistence objects in `Domain/Models` and `Domain/Enums`, matching the current entity/enum folders. Group the related progression entities in one domain model file, consistent with the existing grouped `Community.cs` model file, while keeping Application CQRS untouched because no endpoint or workflow is being added.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Use existing `Player` as the PlayerProfile persistence entity.
- Preserve existing `Experience`, `Level`, and `Gold` fields and defaults.
- Add missing player summary fields directly to `Player`.
- Add progression enums in a new grouped enum file.
- Add progression entities in `Domain/Models`.
- Configure EF Core inline in `ApplicationDbContext.OnModelCreating`.
- Use nullable `CommunityId` on B2C-capable match/progression records.
- Use `Restrict` or `NoAction` delete behavior for player/community/match relationships that could create MySQL cascade cycles.
- Do not normalize question types in this feature.
- Generate migration with `dotnet ef migrations add AddProgressionPersistenceFoundation --project Infrastructure --startup-project API`.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [quickstart.md](./quickstart.md)

External interface contracts are intentionally skipped because this feature has no API, command, query, or frontend contract.

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The slice is schema/domain/migration plus validation.
- **Existing architecture wins**: PASS. No new framework, package, repository, service, or abstraction is planned.
- **SaaS data isolation**: PASS. Nullable community context is explicitly modeled for B2C, with optional B2B community linkage.
- **JSON where flexibility matters**: PASS. Question payload storage remains unchanged.
- **Minimum useful implementation**: PASS. All calculation and read/write workflows are excluded.
- **Quality gates**: PASS. Build and migration inspection are part of quickstart validation.
- **Documentation is executable context**: PASS. The artifacts describe exactly what implementation and verification should cover.

## Complexity Tracking

No constitution violations.
