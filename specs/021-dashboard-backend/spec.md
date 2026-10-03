# Feature Specification: Community dashboard backend
**Feature Branch**: `codex/021-dashboard-backend`
**Created**: 2026-10-03
**Status**: Implemented; environment validation pending
**Input**: Implement the remaining frontend Markdown requirements using existing backend conventions. Keep predefined grades 7–12; omit enrollment percentage/term progress; defer start-now and exports.

## User Scenarios & Testing
### User Story 1 — View scoped classes and students (P1)
Owners view their community; teachers view only assigned classes. Both can inspect rosters without exposing another class/community. Game activity, score and results are deferred.
**Independent Test**: A teacher assigned to class A can list A's students but cannot read B or another community.
**Acceptance Scenarios**:
1. Given active assignments, listing classes returns only authorized classes with grade, teacher list and student counts.
2. Given no game integration, numeric scores/session metrics use documented zero placeholders.
3. Match/result reads and question/quiz schema changes are deferred.
4. Given no observations, missing activity dates are null, numeric counts/scores are zero placeholders, and integration limitations are disclosed.

### User Story 2 — Manage classes and teachers (P2)
Owners create/update/archive classes, assign teachers, inspect teacher details/statistics/activity, edit supported profiles, and remove community teachers.
**Independent Test**: Update a class assignment without altering another class; reject a teacher/grade from another community.
**Acceptance Scenarios**:
1. Class create/update keeps existing behavior and predefined grade validation; optional assignment is atomic.
2. Existing multiple-teacher assignments remain supported; lists expose all teachers.
3. Teacher title is TEACHER or LEAD_TEACHER; deterministic teacher codes use account identifiers.
4. Class archive maps to existing soft deletion; deactivation maps to existing community removal and license release.
5. Teacher profile name/title can be edited; email and derived grade remain read-only until their supporting workflow is defined.

### User Story 3 — Manage invitations and communicate (P2)
Owners list pending/expired invitations, resend or revoke safely, and send an internal message to a community teacher. Staff view their own notifications.
**Independent Test**: Reject cross-community invitation mutation and notification access; cancellation releases a pending teacher's seat exactly once.
**Acceptance Scenarios**:
1. Pending invitations expire after the existing configured lifetime (default seven days); accepted/revoked records are excluded.
2. Resending invalidates old tokens and preserves class assignments.
3. Revoking an obsolete invitation cannot remove an active teacher or revoke its replacement invitation.
4. Teacher messages persist recipient-specific notifications and activity records; no email sending is introduced for messages.

### User Story 4 — View dashboards, identity and search (P3)
Owners see community counts/date-range metrics and teacher activity; teachers see assigned-class summaries/weekly activity. Staff search authorized entities using existing class/student/teacher records.
**Independent Test**: Verify week boundaries and empty baselines; search cannot expose unassigned students.
**Acceptance Scenarios**:
1. Role/title/notification count are available through a staff identity projection without breaking the existing game identity API.
2. Dashboard numeric gameplay fields are zero placeholders pending game integration; teacher/admin payloads follow their separate frontend shapes.
3. New-student counts use license creation in the current Monday–Sunday UTC week; session/period comparisons are deferred.
4. Quiz options and game session/results work are deferred while the user updates question schema.

### Edge Cases
- Invalid page/date/status/sort/title inputs; dates without observations; zero denominators.
- Removed/suspended users, archived classes, pending teacher assignments, multiple teachers/grades.
- Cross-tenant IDs, duplicate email, obsolete invitations, cancellation retries.
- Incomplete/legacy matches, invalid timing, absent player identity, and no persisted gameplay coverage.

## Requirements
### Functional Requirements
- FR-001: Preserve existing identity, grading, licensing and authentication behavior.
- FR-002: Enforce community ownership and assigned-class teacher visibility on every new read/write.
- FR-003: Provide class lists/details/students/rosters with bounded pagination, search/filter/sort.
- FR-004: Provide teacher list/detail/stats/activity, supported profile edits and existing class reassignment/removal.
- FR-005: Provide invitation list/resend/revoke using current invitation authentication policy.
- FR-006: Provide internal messages and recipient-isolated notification list/unread count.
- FR-007: Provide owner/teacher dashboards, authorized search without question schema changes.
- FR-008: Match the frontend Markdown response fields, using zero placeholders for deferred numeric gameplay metrics and null for missing dates. Document that zero placeholders are not measured gameplay. Separate teacher/admin dashboard shapes; owner class responses retain plural teachers, teacher views show assigned classes only.
- FR-009: Preserve fixed grades; no grade creation, enrollment percentage, term progress, match creation/scheduling/options/results, quiz schema, exports, deployment or Unity runtime changes.
- FR-010: Add focused ownership/integrity/aggregation regression tests and frontend mapping documentation.
### Key Entities
- Community/class/student license: current roster and permission scope.
- Teacher membership/assignment: title, seat state and class visibility.
- Existing match/question models remain unchanged and outside this implementation.
- Invitation: lifecycle-owned one-use credentials; never return token material in management lists.
- Notification/activity: community-scoped messages and observed management events.

## Success Criteria
### Measurable Outcomes
- SC-001: Every included frontend action maps to an existing or added documented operation.
- SC-002: All tested cross-community and unassigned-class accesses fail without data disclosure.
- SC-003: Repeated cancellation releases at most one license seat.
- SC-004: Empty and zero-baseline reports return documented zero placeholders without divide-by-zero errors or claiming gameplay integration exists.
- SC-005: Existing automated regression checks continue to pass.

## Assumptions
- Existing Owner maps to frontend COMMUNITY_ADMIN; Teacher maps to TEACHER.
- Keep multiple teachers per class for owner responses. Teacher views show assigned classes only; teacher grade labels join assigned grade names.
- Deactivation means existing community removal; re-invitation is the restoration path.
- Messages are in-app notifications. No actual external message is sent during implementation.
- Student activity is UNKNOWN until game observations arrive; teacher details expose observed management activity; teacher activity session/date fields remain placeholders, not inferred matches.
- All match creation/scheduling/options and game result reads wait for later integration.
- Question schema and quiz choices are deferred at user request.
