# SprintLabs current state

Last documentation review: 2026-10-30. This is shared repository context. Verify current source, branches and environments before acting.

## Architecture and authority

.NET 8: API -> Application/Infrastructure; Application -> Domain; Infrastructure -> Domain; Domain -> Shared. Follow AGENTS.md: thin controllers, MediatR slices, existing repositories, EF Core/MySQL and BaseResponse envelopes.

The backend owns persisted identity, wallet, XP, level, rank, inventory, mission progress and question history. Trusted Mirror servers report match facts. Player clients cannot award themselves rewards. Platform administration is checked against database users; community access uses active scoped membership and roles.

## Feature map

| Area | Source contracts |
|---|---|
| Identity, communities, teachers, students | Relevant `specs/001-*` through `specs/025-*`; later features extend earlier ones |
| Development player login | `specs/020-production-like-dev-auth/api.md` |
| Relational questions and typed history | `specs/026-question-data-scheme/api.md` |
| Match registration/completion and progression | `specs/022-match-progression-apis/api.md`, `specs/027-game-postmatch-integration/api.md`, current services/tests |
| Weekly/seasonal missions, claims/history | `specs/023-missions-system/api.md` |
| Bundled profile/avatar selection | `specs/028-bundled-profile-appearance/api.md` |
| B2C nickname/grade/curriculum/language | `specs/029-b2c-player-setup/api.md` |
| Coin shop, inventory and equipment | `specs/030-coin-shop/api.md`, `specs/030-coin-shop/game-integration.md` |

Older specs can describe earlier delivery stages. Resolve disagreements against actual controllers, DTOs, services and tests, then correct stale documentation.

## Important current behavior

- Anonymous GET `/api/v1/Question` reads relational Questions and typed tables, preserving `data.payloadJson.Questions` for the game. No legacy fallback. Assignment filters are rejected. Publish using admin PUT `/api/v1/Question/bank`; old root PUT maintains legacy packs only.
- Educational tooling generates shared question UUIDs once. Preserve question/answer identities across services. Question timers and typed history durations use seconds; some legacy match summary fields explicitly use milliseconds. Follow each DTO's units.
- Match registration/completion requires GameServer authentication; normal JWTs do not authorize trusted match facts. Completion is bulk per match with retry protection. Preserve stable registration/request identities.
- B2C setup reuses Players.Name/Grade, adding Curriculum/Language. Community-managed players cannot overwrite community setup through the B2C form.
- Shop uses the existing Gold wallet and inventory. Permanent purchases require a stable requestId for retries and do not auto-equip. Asset keys map to bundled Unity assets. No default shop listings are automatically seeded.
- Mission boxes and direct character shop listings are distinct. Do not convert existing non-character reward items through shop tooling.

## Recorded rollout and limits

B2C setup and shop were merged through backend PR #44 into `development` as `741bc2d0d62b3aaaebeb09c1e102dcb27b1e94ab`. Staging workflow run `40801951920` succeeded on 2026-10-29. Both migrations and three example education pairs were verified. No shop seed or test purchase was executed during that deployment verification; recheck current content rather than assuming it remains empty.

Combined suite at that rollout: 700 passed, 13 opt-in MySQL tests skipped, 0 failed. Skipped tests do not establish live MySQL concurrency correctness. Backend deployment alone does not establish Unity integration: inspect the game's current source and PROJECT_HANDOFF.md.

## Environments and working copies

- Backend: `https://github.com/MostafaElsadaad/SprintLabsBE`; staging branch `development`.
- Unity: `https://github.com/MostafaElsadaad/SprintLab`; inspect its current branch, AGENTS.md and PROJECT_HANDOFF.md.
- Staging API: `https://noxed.ddns.net`; local example API: `http://localhost:5153`. Backend API and Mirror server addresses are separate settings.
- Paths vary by workstation. Never assume another developer's drive letters, credentials, branch or dirty files are available.

See [the developer guide](game-developer-guide.md) for local setup, seeds and cross-repository changes.

## Maintaining this memory

Update this file after material backend feature/integration/deployment changes, with factual behavior, links, validation and known limits. Update owning API/frontend docs too. When Unity changes, follow its PROJECT_HANDOFF.md rules. Distinguish implemented, tested and deployed states. Never record credentials, personal player data or copied conversation transcripts.

## 2026-10-30 - Development school assignment (local branch only)

Branch codex/development-school-assignment adds an explicit platform-admin setup action
POST /api/v1/admin/development-players/{accountKey}/school for existing catalogue accounts,
under the existing non-Production development guard/key. It creates/activates a matching
Student license/membership using existing serializable enrollment transactions and seat
capacity. Stable tuple retries reuse the license; role/identity/enrollment conflicts are
rejected. Provider identity, player IDs, progression and normal login remain unchanged.
See specs/020-production-like-dev-auth/api.md and frontend.md. Twenty-one new SQLite/HTTP
checks pass. No schema or package changes. Not deployed; no live assignment performed.

## 2026-10-31 staging deployment recovery and demo leaderboard fixture

User merged PR #47, including the persisted-lockout regression without its UserService fix.
Release tests reproduced one failure. Restored the complete identity mapper as 925db67, then
merged PR #48 into development as f50316b after reconnecting GitHub securely. Staging workflow
completed successfully. Prior failed workflow stopped at Test before changing the live container.
No credentials are in this document, source or diagnostic outputs.

Local SeedLeaderboardDemo action now prepares eight existing catalogue students across three
classes in an explicitly chosen active school/grade. It reuses capacity-aware enrollment and
normal calculated match rewards; three tagged synthetic matches dated to UTC period starts
provide Year/Month/Week test data without resetting progression. Stable per-period keys prevent
duplicate rewards; fresh per-match scopes preserve interleaved normal rewards. Production and
ordinary users are refused by existing guard/admin checks. Docs: specs/020-production-like-dev-auth.
Ten fixture tests passed (including SQLite); no migration or package change. Not yet published
or invoked on staging. Next: final Release suite/review, publish, invoke once for Demo School
community 6 / Grade 7 ID33 / base Class7A ID1, and verify actual leaderboard/class subsets.