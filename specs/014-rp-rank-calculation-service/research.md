# Research: RP and Rank Calculation Service

## Decision: Mirror the XP and level service layering

**Rationale**: The current repository places calculation interfaces in `Domain/Services`, implementations in `Infrastructure/Services`, Shared output contracts in `Shared/Responses`, registrations in `Infrastructure/ServiceConfig.cs`, and pure tests under `SprintLabs.Tests/Features`. Following this structure preserves the documented dependency direction.

**Alternatives considered**:
- Put the interface in Application: rejected because Infrastructure must not reference Application.
- Put implementation in Domain: rejected because Domain contains service contracts, not implementations.
- Use a static helper: rejected because existing reusable progression calculations are interface-based and DI-registered.

## Decision: Name the service `IRpRankCalculationService`

**Rationale**: The name explicitly covers both RP arithmetic and rank-tier derivation while matching the `I...Service` naming used by XP and level progression.

**Alternatives considered**:
- `IRpCalculationService`: shorter but understates rank-tier and Immortal-hook responsibilities.
- `IRankService`: too broad and could imply persistence or leaderboard responsibilities.

## Decision: Reuse Domain progression enums unchanged

**Rationale**: `MatchType` and `RankTier` already exist in `Domain/Enums/ProgressionEnums.cs` with the required values. Reusing them avoids duplicate business concepts and avoids unrelated persistence or migration changes.

**Alternatives considered**:
- Add duplicate enums to Shared: rejected because duplicate definitions can drift.
- Move existing enums to Shared: rejected because it would touch multiple existing models and exceed this calculation-only feature.

## Decision: Do not add a Shared request model

**Rationale**: A Shared request could not contain typed Domain enums because Shared references no projects. The Domain interface can accept four clear parameters (`oldRp`, `matchType`, `position`, `totalPlayers`) while retaining typed `MatchType` at the service boundary.

**Alternatives considered**:
- Shared request with integer match type: viable but weakens input type safety without reducing meaningful complexity.
- Generic Shared request parameterized by enum type: rejected as an unnecessary abstraction for four inputs.

## Decision: Store enum values as integers in the Shared result

**Rationale**: The result must be shared by the Domain interface and Infrastructure implementation, but Shared cannot reference Domain. Integer properties named `MatchType`, `OldRankTier`, and `NewRankTier` preserve the existing explicit enum values without adding references or duplicating enums. Future Application callers reference both projects and can cast/map to the existing Domain enums.

**Alternatives considered**:
- Strings: rejected because they are more error-prone and do not preserve the established numeric enum contract.
- Move the result to Domain: rejected because the user explicitly requires cross-layer result models in Shared.
- Add a Shared-to-Domain reference: rejected because it violates the dependency direction.

## Decision: Expose pure formula and tier helper methods

**Rationale**: Direct methods for percentile, BaseRP, loss multiplier, tier derivation, and Immortal eligibility make each business rule independently testable and reusable by future match-completion code. The complete calculation method composes those helpers.

**Alternatives considered**:
- Test only through one complete calculation method: possible, but makes exact formula and multiplier failures harder to isolate.
- Add separate calculator classes per rule: rejected as unnecessary fragmentation.

## Decision: Derive old tier from old RP

**Rationale**: This prevents inconsistent inputs such as Student with 4000 RP and ensures loss multipliers use the same tier rules as returned results. New tier is derived after applying and clamping RP.

**Alternatives considered**:
- Accept old tier as a separate input: rejected because callers could pass a tier that conflicts with old RP.
- Read PlayerProfile: rejected because the service must remain pure and persistence-free.

## Decision: Report the effective applied RP change

**Rationale**: After rounding, new RP is clamped to zero. Recomputing `RpChange` as `NewRp - OldRp` keeps all returned values internally consistent when the calculated loss exceeds available RP.

**Alternatives considered**:
- Return the uncapped calculated loss: rejected because `OldRp + RpChange` would not equal `NewRp`.
- Add both calculated and applied loss fields: rejected because the required result only needs one RP change and extra fields are speculative.

## Decision: Non-ranked modes bypass ranked placement validation

**Rationale**: Friendly and Private matches do not calculate RP, so invalid or absent ranked placement context should not block an unchanged result. Old RP remains validated because negative RP is invalid in every mode.

**Alternatives considered**:
- Validate position and player count for every mode: rejected because those fields do not affect the non-ranked result and the specification explicitly excludes ranked calculation there.

## Decision: Immortal is a false-only hook

**Rationale**: Immortal requires global Top 10 Legend data that this pure service does not have. RP tier derivation therefore stops at Legend, and a method/property reports false until leaderboard-aware logic is introduced.

**Alternatives considered**:
- Assign Immortal above a higher RP threshold: rejected because Immortal is position-based, not RP-threshold-based.
- Add leaderboard dependencies: rejected as explicitly out of scope.

## Decision: Use xUnit and FluentAssertions without test infrastructure

**Rationale**: Existing XP and level service tests instantiate the Infrastructure implementation directly behind the Domain interface. RP/rank logic is pure and needs no mocks, EF provider, controller host, or fixtures.

**Alternatives considered**:
- EF Core tests: rejected because there is no persistence behavior.
- API tests: rejected because no endpoint is added.
