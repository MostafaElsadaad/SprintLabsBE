# Implementation Plan: Level Progression Service

**Branch**: `013-level-progression-service` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/013-level-progression-service/spec.md`

**Note**: This plan is for level progression calculation logic and unit tests only. It intentionally excludes APIs, database writes, migrations, match completion, XP source calculation, RP/rank calculations, leaderboards, login, and community authorization changes.

## Summary

Implement a reusable, deterministic level progression service for future match completion workflows. The service will calculate required XP using `ceil(150 * level^1.25)`, derive old and new levels from total lifetime XP, support multiple level-ups from one XP reward, return old/new XP and level state, return the next-level XP requirement, and expose a placeholder unlock hook.

The implementation follows the current SprintLabs service layering: service interface in `Domain/Services`, implementation in `Infrastructure/Services`, shared request/response models in `Shared`, and DI registration in `Infrastructure/ServiceConfig.cs`. No `Infrastructure -> Application` reference is allowed.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: Existing SprintLabs solution references; no new packages planned

**Storage**: N/A for this feature. No EF Core reads/writes, migrations, or PlayerProfile persistence changes.

**Testing**: xUnit and FluentAssertions in `SprintLabs.Tests`

**Target Platform**: SprintLabs .NET backend

**Project Type**: Backend API solution with API/Application/Domain/Infrastructure/Shared separation

**Performance Goals**: Deterministic calculation in simple loops over crossed level thresholds; no external I/O.

**Constraints**: No APIs, no database writes, no EF Core dependency, no migrations, no MediatR handlers, no controller changes, no login or authorization changes, and no `Infrastructure` reference to `Application`.

**Scale/Scope**: One reusable level progression service plus focused unit tests for formula, level-up, old/new values, next-level requirement, and unlock hook behavior.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: Pass. This feature is intentionally calculation-only; the complete slice is service contract, implementation, shared result models, DI registration, and tests.
- **Existing architecture wins**: Pass. Uses existing Domain service interface, Infrastructure service implementation, Shared model, Infrastructure DI, and xUnit test conventions.
- **SaaS data isolation**: Pass. Feature stores no B2B/B2C data and does not read community or tenant state.
- **JSON where flexibility matters**: Pass. No question JSON changes.
- **Minimum useful implementation**: Pass. No speculative APIs, persistence, rank/RP, match history, or real unlock system.
- **Quality gates**: Pass. Plan requires `dotnet build SprintLabs.sln` and relevant tests.
- **Documentation is executable context**: Pass. Plan, research, data model, internal contract, and quickstart live under `specs/013-level-progression-service`.

## Project Structure

### Documentation (this feature)

```text
specs/013-level-progression-service/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- level-progression-service.md
`-- tasks.md
```

### Source Code (repository root)

```text
Domain/
`-- Services/
    `-- ILevelProgressionService.cs

Infrastructure/
|-- Services/
|   `-- LevelProgressionService.cs
`-- ServiceConfig.cs

Shared/
|-- Requests/
|   `-- LevelProgressionRequest.cs
`-- Responses/
    |-- LevelProgressionResult.cs
    `-- LevelUnlockResult.cs

SprintLabs.Tests/
`-- Features/
    `-- LevelProgressionService/
        `-- LevelProgressionServiceTests.cs
```

**Structure Decision**: Use `ILevelProgressionService` in `Domain/Services` and `LevelProgressionService` in `Infrastructure/Services`, matching the corrected XP calculation service pattern. Put cross-layer input/output objects in `Shared/Requests` and `Shared/Responses`. Register the implementation in `Infrastructure/ServiceConfig.cs`. Do not add or rely on any Application project reference from Infrastructure.

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: Pass. The designed slice contains the calculation service, shared models, DI registration, and tests only.
- **Existing architecture wins**: Pass. The plan mirrors the existing XP service layering and current service registration convention.
- **SaaS data isolation**: Pass. No persisted or tenant-owned records are introduced.
- **JSON where flexibility matters**: Pass. No JSON payload changes.
- **Minimum useful implementation**: Pass. The unlock hook is a placeholder result/list only.
- **Quality gates**: Pass. Quickstart requires build and relevant tests.
- **Documentation is executable context**: Pass. All planning artifacts are under `specs/013-level-progression-service`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations identified.
