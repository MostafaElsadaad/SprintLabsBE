# Implementation Plan: XP Calculation Service

**Branch**: `012-xp-calculation-service` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/012-xp-calculation-service/spec.md`

**Note**: This plan is for calculation logic and unit tests only. It intentionally excludes APIs, database writes, migrations, match completion, RP/rank/level calculations, login, and community authorization changes.

## Summary

Implement a reusable, deterministic XP calculator for future match completion workflows. The calculator will compute answer XP from ordered question outcomes, match result XP from winner/non-winner state, mission XP from supported mission difficulty, and total XP as the sum of those components.

The implementation should stay in the domain/application-service area that best matches current SprintLabs patterns, avoid EF Core/database dependencies, avoid MediatR/controller dependencies, and be covered by focused xUnit/FluentAssertions unit tests.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: Existing SprintLabs solution references; no new packages planned

**Storage**: N/A for this feature. Existing EF Core/MySQL persistence models are inspected only as future consumers.

**Testing**: xUnit and FluentAssertions in `SprintLabs.Tests`

**Target Platform**: SprintLabs .NET backend

**Project Type**: Backend API solution with Domain/Application/Infrastructure/Shared separation

**Performance Goals**: Calculator should be deterministic and linear in the number of ordered question results.

**Constraints**: No APIs, no database writes, no EF Core dependency, no migrations, no MediatR handlers, no controller changes, no login or authorization changes.

**Scale/Scope**: One reusable XP calculator plus focused unit tests for all listed XP rules.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: Pass. This feature is intentionally calculation-only per spec, so the complete slice is calculator logic plus tests.
- **Existing architecture wins**: Pass. Plan uses existing Domain/Application service and enum conventions, plus the existing test project.
- **SaaS data isolation**: Pass. Feature stores no data and does not read tenant/community data.
- **JSON where flexibility matters**: Pass. No question JSON schema changes.
- **Minimum useful implementation**: Pass. No speculative APIs, persistence, rank, RP, or mission system work.
- **Quality gates**: Pass. Plan requires `dotnet build SprintLabs.sln` and relevant tests.
- **Documentation is executable context**: Pass. Plan, research, data model, internal contract, and quickstart live under `specs/012-xp-calculation-service`.

## Project Structure

### Documentation (this feature)

```text
specs/012-xp-calculation-service/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- xp-calculator.md
`-- tasks.md
```

### Source Code (repository root)

```text
Domain/
|-- Enums/
|   `-- MissionDifficulty.cs
`-- Services/
    |-- IXpCalculationService.cs
    |-- XpCalculationService.cs
    |-- XpQuestionResult.cs
    `-- XpCalculationResult.cs

Application/
`-- ServiceConfig.cs

SprintLabs.Tests/
`-- Features/
    `-- XpCalculationService/
        `-- XpCalculationServiceTests.cs
```

**Structure Decision**: Keep the calculator pure and dependency-free as domain rule logic. Add `IXpCalculationService` because existing service patterns use interfaces, keep the stateless implementation and small calculation records in `Domain/Services`, add `MissionDifficulty` under `Domain/Enums`, and register the service in `Application/ServiceConfig.cs` with the existing scoped service-registration style for future consumers. Tests belong under `SprintLabs.Tests/Features/XpCalculationService`.

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: Pass. The designed slice is calculator behavior plus tests, matching the calculation-only scope.
- **Existing architecture wins**: Pass. Uses existing Domain service/enum conventions and existing xUnit test project.
- **SaaS data isolation**: Pass. No community, tenant, or persistence access is introduced.
- **JSON where flexibility matters**: Pass. No JSON/question payload storage changes.
- **Minimum useful implementation**: Pass. No APIs, repositories, migrations, rank/RP/level logic, or mission persistence.
- **Quality gates**: Pass. Quickstart requires build and relevant tests.
- **Documentation is executable context**: Pass. All planning artifacts are under `specs/012-xp-calculation-service`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations identified.
