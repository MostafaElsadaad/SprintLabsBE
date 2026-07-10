# Research: XP Calculation Service

## Decision: Use a pure calculator with no EF Core, MediatR, or controller dependency

**Rationale**: The feature is explicitly calculation-only. The existing `MatchQuestionResult` persistence model contains fields that future match completion can use, but the XP rules only require ordered correctness outcomes. Keeping the calculator pure makes it deterministic, easy to unit test, and safe for future use by match completion logic.

**Alternatives considered**:
- Query `MatchQuestionResult` directly inside the calculator: rejected because it would introduce persistence coupling and make tests heavier.
- Add a MediatR command/query: rejected because no API or application workflow is being implemented.
- Add a repository: rejected because no data access is required.

## Decision: Add a domain service interface and scoped registration

**Rationale**: Existing SprintLabs services use interfaces in `Domain/Services` and implementations registered through service configuration. Future match completion is expected to consume the XP calculator through dependency injection, so an interface keeps it consistent with existing service patterns. The implementation must still remain stateless and dependency-free.

**Alternatives considered**:
- Static helper only: simple, but less consistent with current service interface usage if future consumers use DI.
- Full application feature slice: rejected because there is no request/handler/API boundary.

## Decision: Use minimal calculation input/output models

**Rationale**: The calculator should not require EF entities. A minimal ordered answer outcome avoids exposing persistence models and makes wrong-answer reset behavior explicit. A result model with `AnswerXp`, `MatchResultXp`, `MissionXp`, and `TotalXp` maps to future `MatchRewardResult` fields without writing anything.

**Alternatives considered**:
- Use `MatchQuestionResult` directly: rejected to avoid entity coupling.
- Return only a single integer total: rejected because the spec requires component values to remain independently available.

## Decision: Add a mission difficulty enum only if no existing enum exists

**Rationale**: Search found no existing mission or difficulty enum. The feature needs supported values `Normal`, `Mid`, and `Hard`, and using an enum is consistent with current `Domain/Enums` conventions.

**Alternatives considered**:
- Use strings: rejected because supported values are closed and tests should be compile-time clear.
- Add mission persistence: rejected as out of scope.

## Decision: Unit tests use xUnit and FluentAssertions

**Rationale**: `SprintLabs.Tests` already uses xUnit and FluentAssertions. The XP calculator tests should be fast pure unit tests under `SprintLabs.Tests/Features/XpCalculationService` without database fixtures.

**Alternatives considered**:
- Integration tests with EF InMemory: rejected because no persistence behavior is involved.
- API/controller tests: rejected because no API is added.
