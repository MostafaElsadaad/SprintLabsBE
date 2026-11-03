# Implementation plan

Use existing CQRS, repository and DI patterns (.NET 8/EF/MySQL). No packages/references or new generic services. Files inspected: dashboard response mapper/projection, ClassView/detail, student roster/performance/detail, community profile update/read, Community mapping, demo seeder, current tests. No term/calendar fields or score-period metadata currently exist. Class detail scores are placeholders; student scores are real all-time aggregates.

1. Add Shared CommunityTermRequest/Response (DateOnly dates), Community nullable persisted columns, and Application CommunityTermPolicy for validation/progress/read mapping. Configure bounded name/date columns and constraints; generate isolated migration/snapshot.
2. Extend existing community profile read/update contracts with optional term and controller mapping; preserve authorization and other fields. Update validates before persistence and omission preserves current dates.
3. Project community term once per scoped class query; add term/progress to class list/dashboard/detail (both owner/teacher). Add Application ScoreReportingPeriod next to student scores, and UNAVAILABLE metadata for class placeholder. No new score query.
4. Extend demo seeding when all term fields empty. Verify existing records survive repeated seed; do not fabricate score data.
5. Focused unit/HTTP/schema/seed checks, build/full regression, generated migration SQL/model check and API/frontend/handoff updates.

Testing: first pin missing JSON fields in a failing HTTP test; deterministic direct policy tests at pre-start/start/mid/end/completed dates, same-day and UTC-offset scenarios. Existing exact response-key tests updated only for intended additive fields. Seed checks demonstrate sample term and preservation. Production startup not executed; EF generation uses design-time factory with no database connection.
