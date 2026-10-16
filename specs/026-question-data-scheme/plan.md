# Question data scheme implementation plan

2026-10-16: codex/question-relational-reads removes IBaseRepository<QuestionsJson> and optional-service fallback from GetQuestionsQueryHandler; require IQuestionDataService and ProjectBankAsync exclusively. Reject assignment (including blank query) and return neutral pack headers. Prove physical legacy-table independence for all six types in SQLite-backed HTTP tests, plus legacy-only 404 and unchanged imported/new reads. Keep admin migration tools/old pack storage intact. Validate full backend suite and current grade-only game contract before publishing/deploying through the previously authorised staging workflow.

2026-10-13 follow-up: publish from current development in isolated branch codex/question-import-count, preserving term-zero support and unrelated local work. Extend compatible filtering/projection with count; keep the current game response and no-count behavior. Reuse existing import service and correct equivalent fill-answer variants. Focused/full backend regressions precede publication. Existing staging CI deploys the merged development branch. After deployment verify count=10, no-count/assignment routes, all six payload shapes, metadata filtering and admin import retries.

Goal: Implement the user-approved relational question bank and typed history.
Execution: Inline, requested by the user on 2026-10-11; branch feature/question-data-scheme. No additional approval loop.
Stack: existing .NET 8, EF Core 8, Pomelo MySQL, MediatR, BaseResponse, GameServer authentication. No new packages or project references.
Spec: spec.md and the linked external refined schema.

Architecture: Domain/Models/QuestionData contains one entity per table. Shared/Requests/QuestionData and Shared/Responses contain wire contracts. Domain/Services/IQuestionDataService defines the transactional persistence boundary; Infrastructure/Services/QuestionDataService implements EF operations with existing DbContext, and Infrastructure/DataAccess/QuestionDataModelConfiguration configures tables. Application/Features/Questions uses separate CQRS slices; a thin QuestionDataController exposes bank/history routes. Existing JSON/progression APIs stay unchanged.

- [x] T001: Write failing SQLite physical-schema test. Add 15 entities and model configuration, explicit UUID binary conversion, restrict deletes and uniqueness/indexes. Prove constraints on SQLite.
- [x] T002: Write failing publishing/query tests. Implement immutable idempotent admin publishing, six type validation, deterministic batched reads and client answer-key redaction.
- [x] T003: Write failing history tests. Validate roster membership, wrong-question choices, response shape, timing and atomic inserts; implement paged own/admin/server history export with typed responses and derived correctness.
- [x] T004: CQRS/controller/DI integration and HTTP auth tests. Preserve legacy routes; inspect OpenAPI serialization and cancellation.
- [x] T005: Generate source MySQL migration and offline SQL, verify non-destructive legacy rename and UUID storage. Document APIs/frontend mapping, run focused/full tests, build, inspect scoped/complete diffs and update living handoff.

Review focus: published IDs are never regenerated; falsy Boolean responses remain valid; duplicate drag option text is preserved; matching relations must not reveal correct pair IDs through public display responses; pagination bounds large query results; malformed requests fail without partial writes; ordinary player tokens cannot submit educational facts; source migrations do not assume old numeric IDs equal shared UUIDs.

- [x] T006: Follow-up seconds fields and timing migration; approved dummy legacy conversion/export; old GET response projection and trusted-server full-bank compatibility; fractional-time, publication/replay, privacy and migration tests. Read snapshots protect compatible multi-page reads during publication.
