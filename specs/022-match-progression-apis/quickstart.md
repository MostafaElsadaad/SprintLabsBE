# Local verification and integration

Date: 2026-10-03. Backend: `K:\Projects\DotNet\SprintLabsbkp`.

## Offline checks

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter 'FullyQualifiedName~MatchProgression|FullyQualifiedName~XpCalculationService'
dotnet test SprintLabs.sln
```

Focused tests use ephemeral InMemory/TestHost and in-memory SQLite, with no API startup, real credentials, email, or live database. Test-only SQLite defaults adapt MySQL's CURRENT_TIMESTAMP(6) dialect; production mappings stay MySQL.

Three MySQL concurrency tests additionally require `SPRINTLABS_MYSQL_TEST_CONNECTION` supplied privately in the process environment. Its configured database must start with sprintlabs-test or compass-test. Each test creates/drops a unique sprintlabs-test-progression-* database; credentials need create/drop permissions. No supplied base/production database is deleted. Never print the connection value. Without configuration these tests explicitly skip. SQLite cannot validate MySQL locks.

## Schema changes

- `20261003194331_MatchProgressionApiIntegrity`: unique Matches.MatchCode; Matches.TotalPlayers.
- `20261003195828_MatchAnswerOrdering`: per-player MatchQuestionResults.Sequence; index (MatchId, PlayerProfileId, Sequence).

These migrations were generated as source artifacts. No live migration was applied. Existing API startup automatically applies pending migrations; review/backup/preflight before restarting a database-connected API with this branch.

Preflight existing match data with read-only SQL:

```sql
SELECT MatchCode, COUNT(*) FROM Matches GROUP BY MatchCode HAVING COUNT(*) > 1;
SELECT Id, MatchCode, Status FROM Matches;
```

Duplicate MatchCodes must be resolved explicitly before creating the unique index; this feature does not silently rewrite settled historical identity. Legacy rows get TotalPlayers=0 and existing answers Sequence=0. History falls back to IDs for legacy zero-sequence answers. Do not submit old fixture/legacy started matches for completion without validating/backfilling their exact original roster/total slots through an operator-approved procedure. New registration produces valid totals and explicit sequence.

Generate/review the incremental SQL without applying it:

```powershell
dotnet ef migrations script CommunityDashboardManagement MatchAnswerOrdering --project Infrastructure --startup-project API --output specs/022-match-progression-apis/migration.sql
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project API
```

Configure `GameServer__ApiKey` in the API environment/secret store and the matching credential in the dedicated server's approved server-only process secret mechanism. Use HTTPS and redact Authorization in infrastructure request logs. No config files were changed by this feature. Swagger distinguishes game-server authorization for match writes from Bearer authorization for reads.

## Manual integration scenarios after schema/secret provisioning

1. Ordinary player/admin JWT attempts to register/complete: 401, no stored change.
2. Server registers two authenticated humans using one unique MatchCode; retry returns the original ID; changing roster/type/context returns 409.
3. Ranked completion with correct/wrong/streak facts: expected XP, level, RP; exactly one reward/profile update per human and auditable logs.
4. Complete twice and concurrently from separate API processes: identical saved snapshots, no duplicate XP/RP/logs.
5. Two distinct concurrent matches sharing humans: both rewards preserved; retry any 503 with the same ID/body in a fresh request.
6. Friendly/private match: XP awarded, zero RP change, stored tier preserved.
7. Bot wins/disconnected human has temporary AI: include bot in TotalPlayers/placement, exclude AI actions from human question history, no bot profile/reward record.
8. Teacher reads only assigned active-class leaderboard/students/history; another community and unassigned student are denied. Owner retains completed school history after removal/archive.
9. Result UI can leave match after approximately 3–5 seconds with “Rewards syncing”; server retries; profile refresh shows settled totals once.

Unity server submission/retries and client UI integration are still separate work. No dedicated server or backend deployment was performed here.
