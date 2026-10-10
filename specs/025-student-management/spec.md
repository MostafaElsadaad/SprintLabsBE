# Feature specification: Student management and file exchange

Date: 2026-10-10
Status: Implemented; local validation complete, live environment verification pending.

## Outcome and confirmed scope

Owners and assigned teachers can view accurate enrollment data and stored performance. Owners can bulk enroll students from CSV or Excel and download a filtered roster. The deliverable includes API/frontend documentation and a seeded-account Postman collection with repeatable test data.

The user confirmed that ACTIVE means an active student license, PENDING means awaiting activation, and INACTIVE means revoked. Pending is counted separately. Import uses existing enrollment, skips duplicates and reports each row. Attendance is deferred until its UI requirements are defined. Import/export supersedes the earlier dashboard export deferral for student data only.

Keep versioned Communities routes, BaseResponse envelopes for JSON, numeric Int64 IDs, current staff/community authorization, predefined grades and existing seat/activation rules. Preserve unrelated configuration and earlier Postman changes. No Unity/frontend production edits or live schema migration are included. Git publication and PR merge follow the separately authorized repository workflow.

## Proposed public contract

### GET /api/v1/Communities/students

Preserve `data.items`, `page`, `pageSize` and `total`. Preserve existing row fields `id`, `fullName`, `avgScore`, `sessionsCount`, `status`; add `studentCode`, `email`, `class` (`id`, `name`), `grade` (`id`, `value`, `name`), `licenseStatus`, `joinedAt` and `activatedAt`. `id` remains the student-license ID. The display code is derived as `STU-` plus that ID padded to at least six digits; it is not a new persisted identifier.

`joinedAt` uses the enrollment's CreatedAt; `activatedAt` remains nullable. Pending enrollments without profiles use their enrollment email as the display name, matching existing behavior. Never fabricate a name, activation date or gameplay session.

`status` accepts ACTIVE, PENDING, INACTIVE case-insensitively. These correspond to Active, Pending and Revoked in the database. `licenseStatus` exposes the explicit uppercase database-state label: ACTIVE, PENDING or REVOKED. Unspecified status includes all three states in the permitted class scope. Unsupported values return 400; UNKNOWN no longer means enrollment status.

Keep case-insensitive name/email search, classId/gradeId filtering, bounded pagination and stable sorting. Teachers see only assigned active classes; owners can inspect their community's historical enrollments. Other dashboard aggregates/search previews retain their existing non-revoked behavior.

### GET /api/v1/Communities/students/{studentId}

The default `studentId` is the license ID returned by the list. Explicit `idType=playerProfile` selects the old player-profile lookup for migrating existing consumers. Do not try one identifier and then the other, because numeric ID collisions can resolve to the wrong student. Document this compatibility change and update route-contract tests.

Preserve existing detail fields where applicable, and add the same student code, enrollment status, class/grade and joined date used by the list. Expose email and license state even for pending or revoked enrollments without a player. UserId/PlayerProfileId are nullable when no linked identity exists. Detail authorization must enforce the same assigned-class scope as the list.

Populate performance from completed matches explicitly associated with the student's community and linked player: matches/sessions played, wins, correct/wrong answers, average answer accuracy percentage, win percentage and last completed activity. Preserve existing analytics/progression fields; expose zero for numeric aggregates with no observations and null for last activity. No profile means zero observations. Do not infer school attendance or completed assignments from match participation. Shared gameplay projections outside the requested student APIs are unchanged.

### GET /api/v1/Communities/students/stats

Return `{total, active, inactive, pending}` inside BaseResponse. Total includes all three enrollment states; active + inactive + pending equals total. Support classId/gradeId scope filters and the same name/email search as the roster. Status filtering is omitted from statistics so all status cards are calculated from the same population. Teacher/community restrictions match the roster.

### POST /api/v1/Communities/students/import

Owner only. Accept multipart upload field `file`, supporting UTF-8 CSV and modern Excel `.xlsx`. Required columns are `email`, `gradeId`, `classId`; worksheet 1 is used for Excel. Do not accept `.xls`, macros, passwords or executable formulas. Provide sample CSV/XLSX files with the Postman artifacts, without adding a template endpoint.

Maximum uploaded size: 5 MiB; maximum data rows: 1,000. Limit decompressed XLSX XML/content and worksheet dimensions before parsing. Handle quoted commas/newlines, UTF-8 BOM and ordinary Excel shared/inline strings. Reject malformed files, missing/ambiguous headers and oversized content before processing any enrollment. Extra columns do not mutate student accounts or supply identity/trust values.

Each valid row uses the existing enrollment rules: active owner, matching community grade/class, available seat, existing non-revoked email protection and normal activation for an eligible existing account. Never create an account/password or bypass seats. Duplicate non-revoked enrollment emails, including repeats within a file, are skipped; revoked enrollments follow existing enrollment rules. Invalid rows report a reason while other rows continue. Row failures must not leave partial membership/seat changes. Concurrent capacity/duplicate protection must preserve seat limits.

Return counts `{totalRows, imported, skipped, failed}` and ordered row results with row number, email, outcome, created license ID when successful, and a safe reason/code otherwise. HTTP 200 means the file was processed; row-level failures remain visible. Authorization failures are 401/403; malformed/oversized file responses use the established API error envelope (400/413 as appropriate). Infrastructure failures are not mislabeled as successful row imports.

### GET /api/v1/Communities/students/export

Owner and assigned teacher. Accept `format=csv|xlsx` (default csv), search, status, classId and gradeId. Export all matching permitted rows, independently of the UI page, with a documented maximum of 10,000 rows; refuse excess rather than silently truncate.

Return a downloadable file with correct media type, safe filename and Content-Disposition. Include student code, name, email, enrollment/license status, class/grade identifiers/names, joined/activation dates and available performance aggregates. Use UTC ISO dates, CSV quoting and spreadsheet-formula protection for text fields. XLSX text cells must remain text. No hidden identity credentials, account tokens or signing/database information appear in files.

## Approaches considered

1. Extend existing student projections and enrollment flow (recommended). Keeps routes, authorization and seat semantics together; requires documented detail-ID migration and scoped file parsing.
2. Add a second student API family and separate import enrollment logic. Avoids changing the old detail route, but duplicates contracts and integrity rules; rejected given the user's preference to modify existing APIs.
3. Only support CSV and let Excel open it. Simpler, but does not satisfy actual Excel upload/download support; rejected. Modern XLSX is proposed, with legacy XLS explicitly unsupported.

## Implementation boundaries

Use existing repository/CQRS/DI patterns and one public type per file. Application owns student query/command semantics; Infrastructure owns the file-format adapter and atomic enrollment persistence behind a Domain interface if needed. Reuse existing services rather than introduce a second membership/seat authority. No new packages or project references are approved: evaluate bounded .NET ZIP/XML handling for the restricted XLSX contract; if a library is necessary, present the concrete dependency choice before installing it.

No attendance entity/endpoint, new login flow, enrollment percentage, term progress, match ingestion, question schema, or unrelated security/architecture refactor is included.

## Acceptance and verification

- Roster/detail round-trip by license ID works for active, pending and revoked enrollments, including differing license/player IDs. Explicit player-profile lookup cannot silently cross identity types.
- ACTIVE and other documented status filters succeed; invalid filters return 400. Name/email searches, class/grade filters and pagination remain correct.
- Statistics partition the scoped population and never reveal unassigned classes or another community.
- Completed community match data supplies accurate totals/percentages; empty histories return documented zeros/null dates.
- CSV and XLSX round-trip with commas, quotes, multiline text, Unicode, blank cells, dates and safe text handling. Malformed, oversized/decompression-heavy, invalid-reference, formula and out-of-scope inputs are rejected safely.
- Import twice produces imported rows once and skips existing enrollments thereafter. Invalid rows do not block valid rows or consume seats; capacity and concurrent duplicate/capacity boundaries are verified.
- Export reflects filters and teacher scope and does not truncate silently.
- Focused API/query/import/export tests, solution build and appropriate regression tests pass; unavailable real MySQL checks are reported honestly.
- Postman automatically logs in seeded owner/teacher, discovers grade/class/student IDs, tests reads/status/statistics/detail/permissions, creates fresh controlled test enrollment emails per run, checks import/reimport/row errors and downloads both formats. Provide sample upload files and exact Runner instructions, including any required local file selection. Revoke only enrollments created by that run through existing lifecycle APIs; do not delete or deactivate seeded accounts.
- Postman JSON/scripts and repeated-run setup are validated offline. Live requests and email delivery are not performed without the chosen test environment being established.

## Design review

Confirmed business choices are recorded above. Proposed implementation choices for review: license-ID detail default with explicit legacy selector; stable derived codes; separate pending count; true XLSX support limited to worksheet 1; bounded row/file limits; owner imports; scoped exports; community-only completed-match analytics. The user explicitly requested audit followed by implementation on 2026-10-10. Implementation follows plan.md; results and limitations are recorded in quickstart.md.
