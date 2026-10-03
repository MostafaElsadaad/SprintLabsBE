# Implementation and verification

2026-10-03. Branch `codex/022-match-progression-apis`, base `f26168a`. Jira source epic NOX-84; implemented remaining backend stories NOX-89–92. Publication and transition of these stories to In integration were authorized on 2026-10-04.

## Changed source

- API/Authentication/GameServerAuthenticationHandler.cs: separate fail-closed server credential authentication.
- API/Swagger/GameServerOperationFilter.cs and API/Program.cs: authentication registration and correct Swagger security requirements for server-only writes.
- API/Controllers/MatchesController.cs, ProgressionController.cs, RankingController.cs: 12 versioned endpoint contracts including the registration prerequisite.
- Application/Features/Progression: RegisterMatch, CompleteMatch, GetProgression, GetRanking, GetLeaderboard, GetMatchHistory, GetMatch; each has its own command/query and handler file.
- Domain/Services/IMatchProgressionService.cs and IProgressionReadService.cs: atomic persistence/Identity query boundaries.
- Domain/Services/IXpCalculationService.cs and Infrastructure/Services/XpCalculationService.cs: expose existing per-correct-answer calculation for constant-time XP audit logging; reward rules are unchanged.
- Infrastructure/Services/MatchProgressionService.cs: registration, fact validation, row locks, transaction, reward/profile/log persistence, settled snapshot replay, safe database failures.
- Infrastructure/Services/ProgressionReadService.cs: progression/rank, eligible/paged leaderboards, authorized history/detail queries with educational privacy.
- Infrastructure/ServiceConfig.cs: scoped registrations for both services.
- Domain/Models/Match.cs, MatchQuestionResult.cs: TotalPlayers and per-human answer Sequence.
- Infrastructure/DataAccess/ApplicationDbContext.cs: MatchCode uniqueness and answer query index.
- Infrastructure/Migrations/20261003194331_MatchProgressionApiIntegrity.cs/.Designer.cs, 20261003195828_MatchAnswerOrdering.cs/.Designer.cs and ApplicationDbContextModelSnapshot.cs: reviewed schema artifacts matching the runtime model.
- Shared/Requests: RegisterMatchRequest, CompleteMatchRequest, MatchPlayerResultRequest, MatchAnswerRequest, ProgressionPageRequest.
- Shared/Responses: MatchRegistrationResponse, MatchCompletionResponse, MatchRewardResponse, PlayerProgressionResponse, PlayerRankingResponse, LeaderboardEntryResponse, ProgressionPage, MatchHistoryResponse, MatchPlayerResponse, MatchDetailResponse.
- SprintLabs.Tests/Features/MatchProgression: fixture, completion/read/API/relational/MySQL concurrency tests; existing XpCalculationService tests gain helper boundary coverage.
- specs/022-match-progression-apis: spec, plan, tasks, api, frontend, quickstart, changes, migration SQL, Postman collection/guide.
- Unity PROJECT_HANDOFF.md: factual cross-repository status/next-step update only, preserving previous handoff work.

No packages, project/assembly references, Unity assets/.meta GUIDs, public Unity callbacks or network protocol changed. Existing backend appsettings and feature-021 dashboard/Postman edits plus Unity material edits were preserved.

## Verification

- `dotnet build SprintLabs.sln --no-restore -v quiet`: passed, zero errors; existing warnings remain.
- Focused MatchProgression service/HTTP/SQLite checks passed. SQLite test deliberately fails after SaveChanges SQL writes and verifies full transaction rollback from a separate context; fresh-scope retry then succeeds.
- `dotnet test SprintLabs.sln --no-restore -v quiet`: **469 passed, 5 skipped, 0 failed**. Three new isolated MySQL concurrency tests and two existing MySQL tests skip because the dedicated connection environment variable is absent. Docker daemon is unavailable. Do not treat SQLite as proof of MySQL lock behavior.
- `dotnet ef migrations script CommunityDashboardManagement MatchAnswerOrdering ...`: generated/reviewed incremental SQL; no database execution.
- `dotnet ef migrations has-pending-model-changes ...`: no pending model changes.
- Git scoped patch hygiene and source/status review completed. Untracked feature files are included in review.
- Postman: 13 request definitions and 15 scripts checked offline; no live HTTP run. Registration/completion runner intentionally persists test history/rewards.

## Remaining integration and decisions

Review duplicate legacy MatchCodes before applying the unique index; legacy TotalPlayers/Sequence defaults are documented in quickstart. Provision the dedicated-server credential privately, run MySQL scenarios, then integrate Mirror registration/authoritative human fact capture/completion retries and player result/profile refresh. No live API was started because existing startup auto-migrates. Deployment and live migration remain pending integration work.

Immortal Top 10 remains the existing placeholder; mission rewards/claims are separate. Human attribution must stop during AI control. Teachers' history uses current class assignments; historical class snapshots/corrections and changes to already settled rewards require separate designs. See spec.md for all defaults that can be revisited later.
