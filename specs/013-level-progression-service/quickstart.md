# Quickstart: Level Progression Service

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
- No migrations are created.
- No controllers, APIs, login behavior, community authorization, or database writes are changed.
- `Infrastructure` does not reference `Application`.

## Relevant Tests

Run the level progression tests after implementation:

```powershell
dotnet test SprintLabs.sln --filter LevelProgressionService
```

If the exact test class names differ, run the full test project:

```powershell
dotnet test SprintLabs.Tests\Compass.Tests.csproj
```

Expected test coverage:
- XP required formula for level 1.
- XP required formula uses ceiling.
- Player starts at level 1.
- No XP gain keeps same level.
- Enough XP increases level by 1.
- Large XP gain supports multiple level-ups.
- OldLevel and NewLevel are returned correctly.
- OldTotalXp and NewTotalXp are returned correctly.
- LevelsGained is correct.
- LeveledUp is false when level does not change.
- LeveledUp is true when level increases.
- Next level XP requirement is returned.
- Future unlock hook returns an empty list or placeholder result without failing.

## Manual Review Checklist

- Interface is in `Domain/Services`.
- Implementation is in `Infrastructure/Services`.
- Shared request/response models are in `Shared`.
- DI registration is in `Infrastructure/ServiceConfig.cs`.
- `Infrastructure/Infrastructure.csproj` does not reference `Application/Application.csproj`.
- Service has no EF Core dependency.
- Service does not write `PlayerProfile`, `MatchRewardResult`, or `PlayerXpLog`.
- Service does not calculate XP from answers, RP, rank tier, leaderboard position, or match history.
