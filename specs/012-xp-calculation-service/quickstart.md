# Quickstart: XP Calculation Service

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

## Relevant Tests

Run the XP calculation tests after implementation:

```powershell
dotnet test SprintLabs.sln --filter XpCalculationService
```

If the exact test class names differ, run the full test project:

```powershell
dotnet test SprintLabs.sln
```

Expected test coverage:
- Single correct answer gives 10 XP.
- Wrong answer gives 0 XP.
- Two-answer streak applies 1.2 multiplier on the second correct answer.
- Four-answer streak applies 1.4 multiplier at streak 4.
- Six-answer streak applies 1.8 multiplier at streak 6.
- Eight-answer streak applies 2.2 multiplier at streak 8.
- Ten-answer streak applies 3.0 multiplier at streak 10.
- Wrong answer resets streak.
- Winner receives 50 match result XP.
- Non-winner participant receives 20 match result XP.
- Normal mission gives 25 XP.
- Mid mission gives 50 XP.
- Hard mission gives 100 XP.
- Total XP equals answer XP plus match result XP plus mission XP.

## Manual Review Checklist

- Calculator has no EF Core dependency.
- Calculator has no controller dependency.
- Calculator has no MediatR dependency.
- Calculator does not write `MatchRewardResult`, `PlayerXpLog`, or `PlayerProfile`.
- Calculator does not calculate RP, rank tier, level, leaderboard position, or match history.
