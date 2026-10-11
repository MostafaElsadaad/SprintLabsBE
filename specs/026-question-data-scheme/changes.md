# Implementation and validation

2026-10-11; implementation on feature/question-data-scheme atop e40ec2b. The user subsequently authorised commit, push, PR creation and merge into development. No manual live migration/import was executed; the existing staging workflow and startup migrations may run after merge.

Implemented the 15 agreed tables, shared UUID question primary keys stored in RFC-order binary(16), stable child IDs, immutable/idempotent publishing, all six types, paged display/admin/server reads, atomic educational history, own/admin/server history reads, derived correctness and known-HistoryId replay. User-approved TimerSeconds and TimeTakenSeconds use decimal(10,3).

Changed areas: Domain entities/enum/service contract; Shared wire DTOs; Application CQRS; API QuestionDataController; Infrastructure service/mapping/validation/model configuration/DI; migration/Designer/snapshot; QuestionData tests; feature spec/plan/tasks/API/frontend documents. Living cross-repository PROJECT_HANDOFF.md was updated. Unrelated appsettings/dashboard/Postman/Unity asset work was preserved.

Legacy pack APIs retain their contracts. Source migration renames their table to LegacyQuestionPacks, preserving rows. Numeric MatchQuestionResults remain legacy progression summaries; no educational UUID or missing metadata is invented. The new bank is empty until educational records are published.

## Validation

- Legacy baseline: 74 passed, four MySQL-dependent skips.
- New feature: 40 passed, no failures/skips; SQLite persistence for all types, reference/scope validation, immutable publication, duplicate placements, identifier collision, post-SQL rollback, false responses, HTTP auth/redaction, known-ID replay and migration operations.
- Solution build succeeds. Full suite: 586 passed, 10 skipped, zero failures.
- EF reports no pending model changes.
- Offline SQL: K:/Projects/SprintLab-Artifacts/question-data-scheme/QuestionDataScheme.sql. Upgrade creates 15 new tables and retains the legacy pack table without data deletion.
- Independent review's accepted-answer collation issue was corrected in model/migration/Designer/snapshot and regression checked. Follow-up review reports no remaining concrete findings.

```powershell
dotnet build SprintLabs.sln --artifacts-path K:/Projects/SprintLab-Artifacts/question-data-scheme/build
dotnet test SprintLabs.sln --artifacts-path K:/Projects/SprintLab-Artifacts/question-data-scheme/build --no-build
dotnet ef migrations script 20261003223626_MissionsSystem 20261011010334_QuestionTimingSeconds --project Infrastructure --startup-project Infrastructure --output K:/Projects/SprintLab-Artifacts/question-data-scheme/QuestionDataScheme.sql
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project Infrastructure --no-build
```

Live MySQL migration/concurrency and deployment remain unverified. Existing nullable/default-sentinel warnings are outside this feature. Initial POST after a lost first response cannot guarantee exactly-once creation with the approved columns; known-ID replay is idempotent and ambiguous initial outcomes require reconciliation. Unity integration, educational authoring UI and automatic diagnostics delivery are separate work. Manual checks and coordinated rollout requirements are in api.md.

Follow-up: added seconds-based timer/history, safe source migration 20261011010334_QuestionTimingSeconds, admin legacy conversion with approved dummy metadata and stable identity export, plus compatible GET projection. No live import was performed. Additional tests cover fractional seconds, decimal-scale idempotence, invalid timers, migration value preservation, atomic legacy import/retry, private-UUID protection and old response/authentication compatibility. Independent review's concurrent-publication issue was fixed with a snapshot around compatible bank reads; follow-up review is clear.

User follow-up: existing GET /api/v1/Question is now explicitly AllowAnonymous for new-bank reads, with the old envelope and nested question payload. Removed GameServer gating from that route; publishing/import/history and other typed routes retain authorisation. HTTP regression verifies no header is needed and invalid auth headers do not gate GET. Full regressions remain 586 passed, 10 skipped.
