# Research: Level Progression Service

## Decision: Follow existing SprintLabs service layering

**Rationale**: Current project architecture places service interfaces in `Domain/Services`, service implementations in `Infrastructure/Services`, shared request/response models in `Shared`, and DI registration in `Infrastructure/ServiceConfig.cs`. The recently corrected XP calculation service follows this same pattern.

**Alternatives considered**:
- Put the interface in Application: rejected because Infrastructure must not reference Application.
- Put the implementation in Domain: rejected because Domain must not contain service implementations.
- Put shared outputs in Infrastructure: rejected because future consumers need the contract without depending on implementation.

## Decision: Use `ILevelProgressionService` and `LevelProgressionService`

**Rationale**: Names mirror existing service conventions such as `IXpCalculationService`/`XpCalculationService` and describe the reusable progression rule directly.

**Alternatives considered**:
- `ILevelCalculator`: shorter, but less aligned with service naming in the project.
- Static helper: rejected because existing reusable services are interface-based and DI-registered.

## Decision: Store level progression outputs in Shared responses

**Rationale**: `LevelProgressionResult` is service output shared by the Domain interface and Infrastructure implementation. Existing shared output models live under `Shared/Responses`, including `XpCalculationResult`.

**Alternatives considered**:
- Domain model: rejected because this is not a persisted entity or domain aggregate.
- Infrastructure model: rejected because the Domain interface cannot depend on Infrastructure.

## Decision: Use a minimal shared request model

**Rationale**: `LevelProgressionRequest` can carry `OldTotalXp` and `XpGained` through the service contract without exposing PlayerProfile or persistence models. This mirrors the Shared request/response split used by the XP calculation service input/output.

**Alternatives considered**:
- Method parameters only: viable, but a request model makes future complete-match callers less brittle and keeps old/new XP inputs named.
- PlayerProfile input: rejected because this feature must not couple to database entities or update PlayerProfile.

## Decision: Interpret the formula as per-level advancement cost

**Rationale**: Players start at level 1 and experience is total lifetime XP. To derive level from total lifetime XP while supporting multiple level-ups, the service should treat `ceil(150 * level^1.25)` as the XP required to advance from `level` to `level + 1`, then compare total lifetime XP against cumulative advancement costs.

**Alternatives considered**:
- Treat `ceil(150 * level^1.25)` as a direct total XP threshold for being at `level`: rejected because level 1 would have a threshold even though players start at level 1 with 0 XP, and multi-level advancement semantics would be less clear.

## Decision: Unlock hook returns an empty shared result list

**Rationale**: The feature explicitly excludes a real unlock system. Returning an empty `IReadOnlyList<LevelUnlockResult>` keeps the result shape future-ready without persistence or entitlement logic.

**Alternatives considered**:
- Return strings only: rejected because a small placeholder result type is easier to extend later.
- Implement real unlock mapping: rejected as out of scope.

## Decision: Unit tests use xUnit and FluentAssertions

**Rationale**: `SprintLabs.Tests` already uses xUnit and FluentAssertions. Level progression tests should be pure unit tests under `SprintLabs.Tests/Features/LevelProgressionService` without database fixtures.

**Alternatives considered**:
- EF InMemory tests: rejected because no persistence behavior is involved.
- API/controller tests: rejected because no API is added.
