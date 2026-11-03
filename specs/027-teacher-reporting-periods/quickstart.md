# Verification and local testing

Implemented locally on `codex/027-teacher-reporting-periods`, starting from b37e7b5, on 2026-11-03. Existing appsettings, question-code/migration/spec edits, older dashboard/Postman work and Unity edits were preserved. Publication, merge and staging migration/seed verification were authorized on 2026-11-03; final remote results are recorded in PROJECT_HANDOFF.md.

## Automated results

- Added HTTP assertion first; it failed because term/score metadata was missing, before implementing those fields.
- `dotnet build SprintLabs.sln --artifacts-path "$env:TEMP/SprintLabs-TermReporting" --no-restore -v quiet`: passed, zero errors; existing repository warnings remain.
- Focused term policy/HTTP/seed validation: 14 passed. Includes UTC offsets and inclusive end dates, pre-start/start/midpoint/completed dates, same-day terms, rounding, unknown/invalid legacy data, missing/partial/reversed/overlong input, owner update/preserve/clear, teacher rejection, ISO JSON dates, assigned-class/foreign-school scope, real all-time versus unavailable class metadata and repeat-safe demo seeding.
- `dotnet test SprintLabs.sln --artifacts-path "$env:TEMP/SprintLabs-TermReporting" --no-restore --no-build -v quiet`: **627 passed, 10 skipped, 0 failed** (637 total). The skips are existing integration fixtures, not passing live MySQL checks. Existing SQLite relational tests continue to pass with the new nullable fields/constraint.
- Generated `20261103180226_CommunityTermReporting` from current Release model. Reviewed Up/Down/snapshot: only three nullable Community fields and their check constraint; no question-schema change or school backfill.
- EF `migrations has-pending-model-changes` using Infrastructure's design-time factory: no pending changes. SQL generated offline from `20261012180205_RestorePositiveQuestionTerms` to this migration; only ALTER Communities and migration-history insertion. No database update executed.
- Initial migration scaffold used an outdated normal build and was empty; removed only that newly generated scaffold/snapshot change and regenerated from current Release source. The final generated migration is the reviewed one above.

## Manual test sequence

1. Build/run the updated local backend using its existing configured test database and migration procedure. The API's existing startup migrator applies pending migrations; inspect startup migration results. Existing unrelated local question migrations precede this migration and are not part of this feature. Do not blindly deploy unrelated local work together.
2. With Development and the existing DemoCommunitySeed:Enabled setting, seed startup fills only an empty demo term. It will appear as `Demo term 2026 Q4 (sample calendar)` for a November 2026 first seed, with 2026-10-01 through 2026-12-31 inclusive. Reruns preserve it and any custom dates; no matches or score history are seeded.
3. Log in using the existing seeded owner/teacher accounts. As teacher, GET CommunityDashboard/dashboard and Communities/classes/{assignedClassId}; check termProgress 0–100, matching term dates/name and explanatory definition. Unconfigured other schools should have null term/progress.
4. As owner, GET Communities/me to retain name/slug, then PATCH the same route with those fields and the term example from api.md. As teacher, the same PATCH must return 403. Partial/reversed dates return 400 and preserve the previous term.
5. Send term:{} as owner to clear, then verify teacher class term/progress are null; configure/restore the desired dates afterward. Omission/term:null preserves the configuration.
6. GET Communities/students and Communities/students/{licenseId}. Check scorePeriod.type=ALL_TIME beside the score, label All recorded activity and null unbounded dates. Existing numeric values must not change simply because term dates changed. Class-detail zero placeholders have UNAVAILABLE metadata instead.

No live server was started or restarted for this task, and no stored player/community data was inspected or changed outside isolated tests. Live MySQL migration/seed and UI rendering still need the selected environment. Older exact-key Postman tests need the additive fields documented in api.md; no old collection was modified in this task.

## Publication integration
Publication checkout K:/Projects/DotNet/SprintLabs-teacher-reporting-publish is based on latest origin/development 6ee80c9, including current shop/player/missions/journey work. Scoped feature commit 758d296 from the original checkout was applied without unrelated question/config changes. The migration target-model metadata was regenerated against this current base as 20261103180226_CommunityTermReporting; its Up/Down still changes only community term columns/constraint. Original local branch keeps its prepublication scaffold; do not deploy the old checkout independently. Staging was verified as Development with DemoCommunitySeed__Enabled=true; normal deployment startup applies migrations then runs the existing demo seeder.

Latest-base publication validation: dotnet test SprintLabs.sln --configuration Release --no-restore completed with 787 passed, 15 existing integration skips, zero failures (802 total). EF Release model/snapshot check reports no pending changes. Staging migration and seed verification follows the authorized merge/deployment.
