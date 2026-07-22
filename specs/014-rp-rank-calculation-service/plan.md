# Implementation Plan: RP and Rank Calculation Service

**Branch**: `feature/NOX-88-RP-and-rank-calculation-service` | **Date**: 2026-07-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/014-rp-rank-calculation-service/spec.md`

**Note**: This plan covers pure RP/rank calculation and unit tests only. It excludes APIs, MediatR handlers, persistence, migrations, PlayerProfile updates, match completion, XP/level calculation, leaderboards, real Immortal qualification, login, and authorization changes.

## Summary

Implement a reusable, deterministic `IRpRankCalculationService` for future ranked-match completion. The service will calculate placement percentile and BaseRP, apply rank-sensitive loss multipliers, round with midpoint values away from zero, enforce a zero-RP floor, derive old/new rank tiers, preserve RP for Friendly and Private matches, and expose a false-only Immortal eligibility hook.

The implementation follows current SprintLabs layering: interface in `Domain/Services`, implementation in `Infrastructure/Services`, result contract in `Shared/Responses`, scoped DI registration in `Infrastructure/ServiceConfig.cs`, and focused xUnit tests. Existing `MatchType` and `RankTier` definitions in `Domain/Enums/ProgressionEnums.cs` are reused unchanged.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: .NET standard math APIs and existing SprintLabs project references; no new packages

**Storage**: N/A. No EF Core reads/writes, schema changes, or migrations.

**Testing**: xUnit 2.9.2 and FluentAssertions 6.12.1 in `SprintLabs.Tests`

**Target Platform**: SprintLabs .NET backend

**Project Type**: Backend API solution with API/Application/Domain/Infrastructure/Shared separation

**Performance Goals**: Constant-time, deterministic calculations with no external I/O or collection traversal.

**Constraints**: Infrastructure must not reference Application; Shared must reference no other project; Domain contains interfaces and enums but no implementation; no APIs, MediatR, repositories, EF Core, persistence, migrations, or real leaderboard logic.

**Scale/Scope**: One service interface, one implementation, one Shared result model, one DI registration, and one focused unit-test class.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: Pass. The requested calculation-only slice contains its service contract, implementation, DI registration, and tests.
- **Existing architecture wins**: Pass. Placement mirrors the existing XP and level progression services and preserves current project references.
- **SaaS data isolation**: Pass. The feature has no persistence or tenant/community context.
- **JSON where flexibility matters**: Pass. No question JSON or payload storage changes.
- **Minimum useful implementation**: Pass. One result model and a boolean Immortal hook avoid speculative leaderboard abstractions.
- **Quality gates**: Pass. The plan requires solution build, focused tests, and the full test project when practical.
- **Documentation is executable context**: Pass. Planning artifacts live under `specs/014-rp-rank-calculation-service`.

## Project Structure

### Documentation (this feature)

```text
specs/014-rp-rank-calculation-service/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- rp-rank-calculation-service.md
`-- tasks.md
```

### Source Code (repository root)

```text
Domain/
|-- Enums/
|   `-- ProgressionEnums.cs                 # Reuse MatchType and RankTier
`-- Services/
    `-- IRpRankCalculationService.cs        # Add

Infrastructure/
|-- Services/
|   `-- RpRankCalculationService.cs         # Add
`-- ServiceConfig.cs                        # Add scoped registration

Shared/
`-- Responses/
    `-- RpRankCalculationResult.cs          # Add

SprintLabs.Tests/
`-- Features/
    `-- RpRankCalculationService/
        `-- RpRankCalculationServiceTests.cs # Add
```

**Structure Decision**: Use `IRpRankCalculationService` and `RpRankCalculationService`, matching `IXpCalculationService`/`XpCalculationService` and `ILevelProgressionService`/`LevelProgressionService`. The interface accepts typed Domain `MatchType` parameters directly, so a Shared request model is unnecessary. The Shared result stores `MatchType`, `OldRankTier`, and `NewRankTier` as their stable integer enum values because Shared cannot reference Domain. Future Application consumers can map those values to existing Domain enums without changing dependency direction or duplicating enums.

## Design Details

### Service Contract

`IRpRankCalculationService` will expose narrowly focused methods suitable for direct unit testing and future match completion:

- Calculate one complete RP/rank result from old RP, match type, position, and total players.
- Calculate player percentile from position and total players.
- Calculate BaseRP from percentile.
- Return the loss multiplier for a Student-through-Legend tier.
- Derive a Student-through-Legend tier from non-negative RP.
- Check future Immortal eligibility, returning false in this feature.

### Ranked Calculation Order

1. Reject negative old RP.
2. Derive old rank tier from old RP.
3. For Friendly or Private, return unchanged RP/tier values and `IsRanked = false` without ranked placement validation.
4. For Ranked, require at least two players and a position in the inclusive range `1..TotalPlayers`.
5. Calculate percentile and BaseRP using double precision.
6. Apply no multiplier to non-negative BaseRP; multiply negative BaseRP by the old-tier loss multiplier.
7. Round the calculated change with `MidpointRounding.AwayFromZero`.
8. Clamp new RP to zero and recompute effective `RpChange` as `NewRp - OldRp`.
9. Derive new rank tier from new RP.
10. Return `IsImmortal = false` from the placeholder hook.

### Validation Style

Use `ArgumentOutOfRangeException` for negative old RP, ranked player counts below two, and ranked positions outside the valid range, matching the pure calculation validation style used by `LevelProgressionService`. Use `ArgumentOutOfRangeException` for unsupported enum values. Do not introduce application validation, `GenericException`, or API error handling because this service has no API boundary.

### Testing Strategy

Add pure unit tests with direct service construction, xUnit, and FluentAssertions. Cover formula helpers, first/middle/last placement, every loss multiplier, midpoint rounding, Student loss protection, zero floor and effective delta, non-ranked preservation, all tier boundaries, cross-tier movement, Legend at 8000+, false Immortal eligibility, determinism, and invalid ranked inputs. No mocks, fixtures, database providers, or API test host are needed.

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: Pass. The design includes all requested behavior and verification without unrelated endpoints or persistence.
- **Existing architecture wins**: Pass. Domain, Infrastructure, Shared, DI, and test placement match the two existing progression calculation services.
- **SaaS data isolation**: Pass. No persisted B2B/B2C data or community ownership is introduced.
- **JSON where flexibility matters**: Pass. No JSON work is involved.
- **Minimum useful implementation**: Pass. No request DTO, extra enum, leaderboard model, repository, handler, or generic abstraction is added.
- **Quality gates**: Pass. Quickstart includes focused and broader build/test commands.
- **Documentation is executable context**: Pass. Research, data model, internal contract, and validation guide are colocated with the feature spec.

## Complexity Tracking

No constitution violations identified.
