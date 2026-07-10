# Quickstart: Progression Persistence Foundation

## Purpose

Validate that SprintLabs has the persistence foundation needed for future match completion and progression work, without adding APIs or calculation workflows.

Related artifacts:

- [spec.md](./spec.md)
- [plan.md](./plan.md)
- [research.md](./research.md)
- [data-model.md](./data-model.md)

## Prerequisites

- Existing SprintLabs solution restored.
- EF Core tooling available through the existing Infrastructure/API projects.
- Local configuration can construct the API startup project for design-time DbContext creation.

## Implementation Validation Commands

From repository root:

```bash
dotnet ef migrations add AddProgressionPersistenceFoundation --project Infrastructure --startup-project API
dotnet build SprintLabs.sln
```

If a local database is available and migration application is desired:

```bash
dotnet ef database update --project Infrastructure --startup-project API
```

## Migration Review Checklist

After generating the migration, verify it includes:

- Player progression fields: `Rp`, `RankTier`, `HighestRankTier`, `TotalMatches`, `TotalWins`.
- Existing Player fields `Experience`, `Level`, and `Gold` remain present with defaults.
- `Matches` table.
- `MatchPlayers` table.
- `MatchQuestionResults` table.
- `MatchRewardResults` table.
- `PlayerXpLogs` table.
- `PlayerRankLogs` table.
- Nullable `CommunityId` on B2C-capable records.
- Unique index on `MatchPlayers`: `MatchId`, `PlayerProfileId`.
- Unique index on `MatchRewardResults`: `MatchId`, `PlayerProfileId`.
- Indexes for match lookup, completion/history, player lookup, community-scoped records, XP logs, rank logs, and player progression/ranking fields.
- Delete behavior does not create broad cascade deletion paths from Player, Community, or Match into audit/history records.

## Manual Persistence Scenarios

### 1. Player Defaults

1. Inspect a new Player row or generated migration defaults.
2. Confirm `Experience = 0`, `Level = 1`, `Gold = 0`, `Rp = 0`, `RankTier = Student`, `HighestRankTier = Student`, `TotalMatches = 0`, and `TotalWins = 0`.

### 2. B2C Match Persistence

1. Create or inspect a Match with `CommunityId = null`.
2. Create or inspect related MatchPlayer, MatchRewardResult, PlayerXpLog, and PlayerRankLog rows with `CommunityId = null`.
3. Confirm no CommunityUser or StudentLicense row is required.

### 3. B2B Match Persistence

1. Create or inspect a Match with a valid `CommunityId`.
2. Create or inspect related participant, reward, XP log, and rank log rows with the same community context.
3. Confirm records are linked by nullable community context, not by membership/license requirements.

### 4. Idempotency Constraints

1. Attempt to persist two MatchPlayer rows with the same MatchId and PlayerProfileId.
2. Confirm the duplicate is rejected.
3. Attempt to persist two MatchRewardResult rows with the same MatchId and PlayerProfileId.
4. Confirm the duplicate is rejected.

## Expected Non-Changes

- No controllers or endpoints.
- No Application commands, queries, handlers, requests, or responses.
- No XP, level, RP, reward, or rank calculation.
- No leaderboard or match history workflow.
- No real analytics, reports, dashboards, missions, payments, parent accounts, or game-server integration.
- No Google login changes.
- No community authorization changes.
