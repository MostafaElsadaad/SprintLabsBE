# Quickstart: RP and Rank Calculation Service

## Prerequisites

- .NET 8 SDK
- Existing SprintLabs solution restored

## Build

From the repository root:

```powershell
dotnet build SprintLabs.sln
```

Expected result:
- Build succeeds.
- No migration is created.
- No API, MediatR handler, repository, EF Core behavior, login, or authorization code changes.
- `Infrastructure/Infrastructure.csproj` has no Application project reference.

## Relevant Tests

Run focused tests after implementation:

```powershell
dotnet test SprintLabs.sln --filter RpRankCalculationService
```

Then run the test project to check backward compatibility:

```powershell
dotnet test SprintLabs.Tests\Compass.Tests.csproj
```

Expected test coverage:
- First, middle, and last percentile values.
- BaseRP formula.
- Positive first-place and negative last-place ranked outcomes.
- Student through Legend loss multipliers.
- Midpoint rounding away from zero.
- Student loss protection.
- New RP zero floor and effective applied change.
- Friendly and Private RP preservation.
- Every Student-through-Legend tier boundary.
- Student-to-Seeker and other cross-tier changes.
- 8000 or more RP remains Legend.
- Immortal eligibility is always false.
- Ranked total-player and position validation.
- Negative old RP validation.

## Manual Review Checklist

- Interface is in `Domain/Services/IRpRankCalculationService.cs`.
- Implementation is in `Infrastructure/Services/RpRankCalculationService.cs`.
- Result model is in `Shared/Responses/RpRankCalculationResult.cs`.
- Existing `MatchType` and `RankTier` enums are reused without duplication or relocation.
- Shared does not reference Domain; result enum values are represented by their stable integer values.
- DI registration is in `Infrastructure/ServiceConfig.cs`.
- Service has no Application, EF Core, repository, or database dependency.
- No PlayerProfile, PlayerRankLog, MatchRewardResult, leaderboard, XP, or level logic is added.
- No APIs, migrations, Google login, or community authorization behavior are changed.
