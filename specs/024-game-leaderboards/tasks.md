# Tasks
- [x] T001 Inspect current contracts, instructions, authorization, persistence and tests; create feature branch.
- [x] T002 Extend paged leaderboard and add own-standing endpoints/queries.
- [x] T003 Implement UTC period scoring and enrolled-player scope without changing history/progression access.
- [x] T004 Verify authorization, ties, page-independent standing, periods and relational translation.
- [x] T005 Build/test, document API/frontend usage, review diff and update handoff.

Validation: solution build passed (existing warnings); focused progression/leaderboard 67 passed, four skipped; full suite 519 passed, ten skipped, zero failures. MySQL execution requires an isolated configured test database and remains unverified. No schema migration or manual deployment is required for this feature. Publication is tracked through the feature pull request.
