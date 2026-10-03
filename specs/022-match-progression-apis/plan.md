# Implementation plan

Date: 2026-10-03. Spec: [spec.md](spec.md).

1. Verify NOX-84 child stories against existing progression models/calculators and repository architecture.
2. Deliver NOX-89: server credential authentication; registration prerequisite; bounded completion contract; immutable context/roster validation; transaction + database row locks; profile/fact/reward/log persistence; replay.
3. Deliver NOX-90: progression/ranking self and authorized player reads, using persisted identity and community scope.
4. Deliver NOX-91: suspended-user eligibility joins before pagination; deterministic global/community leaderboards; current grade/class and teacher-assignment filtering.
5. Deliver NOX-92: completed self/community/player history and scoped details, preserving educational-data privacy and school-owned historical access.
6. Add focused InMemory/TestHost tests, SQLite relational rollback/read tests, isolated optional MySQL concurrency tests; run focused checks and solution regression tests.
7. Review migrations, SQL, API/frontend docs, all scoped diffs, and project handoff.

## Boundaries

API owns authentication/HTTP concerns and thin controllers. Application contains one command/query and handler per operation. Domain service contracts and Shared DTOs preserve current references. Infrastructure implementations own the atomic persistence workflow and reused database/Identity eligibility joins; these are meaningful technology boundaries, not one-line CRUD repositories. No feature-specific repository or package/project reference is added.

Reuse existing XP/level/RP services. Expose the existing single-answer streak calculation for O(1) per-answer audit logging. MatchCode uniqueness and TotalPlayers support registration integrity and bot placements. Explicit per-player answer Sequence and a query index preserve chronology independent of generated identity insertion order. MySQL timestamps normalize registration start to datetime(6) precision for retry equality.

MySQL serializable transactions lock Matches before examining completion and Players in ascending ID order before reading balances. Serializable range locking + unique MatchCode arbitrate registration. Database failures return safe 503; the game server retries in a fresh scope using the same match key/ID. No process-local mutex/cache replaces database authority.

Read filters join Identity status in SQL, avoiding pagination gaps/leaks from filtering after paging. Collections use split queries for history/detail to avoid participant/reward Cartesian row amplification. Platform admin, owner, assigned teacher, and participant views have explicit scopes.

## Validation limits

SQLite verifies SQL query translation, foreign keys, transactional rollback, new-scope replay and order, but cannot prove MySQL row-lock behavior. MySQL tests require an explicit dedicated test connection and privileges to create/drop unique test databases. No production database is used. Changes to an already completed result and historical class snapshots are deferred.
