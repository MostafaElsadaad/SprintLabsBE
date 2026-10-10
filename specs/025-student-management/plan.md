# Student management implementation plan

Goal: Complete existing student views/filtering and add statistics, safe CSV/XLSX exchange and repeatable Postman verification.
Architecture: Extend existing dashboard/student CQRS and reuse AddStudentLicenseCommand for every import row. File encoding is an Infrastructure adapter behind a Domain interface; a shared enrollment transaction protects existing enrollment and imports.
Stack: .NET 8, existing EF/Identity/MediatR; built-in CSV text and ZIP/XML XLSX only. No packages/references or schema changes planned.
Spec: spec.md
Execution: Inline, as requested on 2026-10-10. User explicitly requested audit followed by implementation; proceed without another design approval loop. No deployment topology change. Feature publication and PR merge were subsequently authorized on 2026-10-11.

## Audit
- List and detail exist; search already supports name/email.
- List status only accepts UNKNOWN, and projection drops revoked enrollments and required identity/class/date fields.
- Detail accepts player IDs rather than list license IDs and has placeholder analytics.
- Stats/import/export/attendance routes do not exist. Attendance is intentionally deferred.

## Tasks and checks
- T001: Failing roster status/response and missing-route tests; extend StudentView/projection, add scoped StudentRoster query service and community completed-match aggregation. Keep other dashboard consumers non-revoked.
- T002: Stats and detail CQRS slices, explicit idType selector, shared assigned-class scope; tests cover mismatched IDs, pending/revoked, statistics partitions, foreign scope and match attribution.
- T003: Domain IStudentRosterFileService and Shared file DTOs, Infrastructure CSV/XLSX reader/writer; test round-trip Unicode/quoting, headers, formulas, limits, compressed XML and invalid formats before codec implementation.
- T004: Import command reuses existing enrollment command per row; owner authorization before parsing. Domain IStudentEnrollmentTransaction wraps existing enrollment under serializable community-license lock and rolls back row failures/clears failed tracking. Tests cover duplicates, row errors, seat capacity, membership rollback and transaction behavior.
- T005: Export query supports scope/filters and 10,000-row cap; controller handles streamed download and bounded multipart upload, tests inspect HTTP authorization and file media/content.
- T006: Document exact APIs/mappings, generate seeded Postman collection plus CSV/XLSX fixtures with UUID placeholders and cleanup. Syntax/dependency checks; focused tests, build, full regressions, diff review and handoff update.

## Review focus
- Numeric ID collisions: no implicit license/player fallback.
- Revoked rows must not leak into unrelated dashboard counters.
- Spreadsheet text starting with formula markers and hostile XML/ZIP expansion.
- Failed import rows after identity writes must roll back seats/membership; concurrent writers lock the same license row.
- Teacher imports denied; details/export/statistics cannot reveal unassigned classes.

## Rulings
- Keep legacy detail handler tests/contract available internally, but current public detail uses explicit scoped license lookup; idType=playerProfile selects scoped legacy-ID lookup.
- No school attendance route or fake attendance entries.

## Completed review corrections
- Add/revoke/admin capacity/pending-email update handlers share the registered serializable community-license transaction to prevent lost counters and update/import email races. Request-context tracking is cleared after lock acquisition to avoid stale values across import rows. Existing unit-test constructor calls retain optional transaction compatibility; production DI resolves the transaction.
- Workbook full cell references and row suffixes are checked against worksheet bounds and containing row, and declared dimensions are bounded.
- Read-only independent review found these concurrency/reference issues; corrections have direct SQLite concurrency and malformed-file regression coverage. Final focused correction review found no remaining findings. MySQL live concurrency is still unverified.
