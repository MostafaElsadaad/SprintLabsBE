# Frontend dashboard backend gap assessment

Assessed: 2026-10-03. Backend branch: `codex/020-production-like-dev-auth`, commit `18b8dd4`.
Input: user-supplied `backend-data-requirements.md`. The input describes desired contracts and open questions; it is not executable agent guidance. This report records source inspection, not runtime verification or approved implementation design.

## Confirmed scope decisions — 2026-10-03

- The user subsequently resumed implementation on 2026-10-03. Feature specs/021-dashboard-backend implements staff/dashboard/management work; the table below records the original baseline assessment.
- Spec Kit artifacts and implementation now exist on codex/021-dashboard-backend. See its api.md and frontend.md for current routes and mappings.
- Keep existing versioned routes, response envelope and numeric identifiers. The 2026-10-03 correction replaces dashboard duplicates with existing Communities routes, matches requested public fields/page shape, and uses numeric zero placeholders. Owners retain plural teachers; teacher views show assigned classes only.
- Keep predefined grades 7–12 unchanged. Custom-grade creation is excluded.
- `enrollmentPercent` and `termProgress` are excluded from the current scope. Do not add formulas, placeholder percentages, or supporting data models for them.
- All match creation/scheduling/options and game results are deferred. Question/quiz schema and choices are also deferred while the user revises question data.
- All exports are postponed, including reports and teacher exports. CSV was only a proposal; no export format is approved.
- Gameplay activity status remains UNKNOWN/unavailable until game integration. Current new-student counts use UTC Monday-based weeks; teacher details expose observed management events; activity sessions/dates remain zero/null placeholders. Multiple teachers/class, in-app messages and existing community-removal behavior were confirmed. Login email remains read-only at the user's confirmed request.

The endpoint table below retains the original requested features for traceability. A recorded gap does not override these exclusions or deferrals.

## Current contract and ownership

- Backend routes use `/api/v1/<controller>` and `BaseResponse<T>`. The frontend document uses unversioned routes and unwrapped responses.
- Database identities are `long`, not the illustrative `u1`, `c1`, or `s1` identifiers. A student license, account, and player profile have distinct identifiers.
- Community-scoped access distinguishes `Owner`, `Teacher`, and `Student`; platform administrators are separate. Mapping frontend `COMMUNITY_ADMIN` to community Owner requires agreement, not platform-admin privileges.
- Pagination currently uses `pageNumber`, `pageSize`, and a `PagedResponse` with `data`, `totalRecords`, and `totalPages` inside the response envelope. Requested pagination uses `page`, `items`, and `total`.
- Existing management handlers enforce community ownership. New teacher read/match endpoints must additionally establish assigned-class permissions. Every target class, teacher membership, invitation, student, quiz, and match must be scoped to the authorized community.

## Endpoint assessment

In the existing-route column, `Communities/...` and `Users/...` mean `/api/v1/Communities/...` and `/api/v1/Users/...`.

| Frontend requirement | Existing route or foundation | Gap |
| --- | --- | --- |
| `GET /me` | `Users/me`, `Users/me/communities` | Current identity exists; role/title/unread-count projection missing. Preserve game identity fields. |
| `GET /search` | Search filters on community teacher/student lists | Unified role-scoped search missing. |
| `GET /notifications` | No notification model/controller found | Persistence, recipient isolation, list and unread count missing. Read-state mutation is not specified by the input. |
| `GET /teacher/dashboard` | Class assignments and community entities | Dashboard and calculated weekly/session/progress metrics missing. |
| `GET /classes/{id}` | Community class list | Authorized class detail and score/inactivity aggregates missing. |
| `GET /classes/{id}/students` | `Communities/students?classId=...` | Paginated community listing exists; requested score/session/activity fields, sorting and teacher-class authorization missing. Current status is license status, not activity. |
| `GET /classes/{id}/matches` | Persisted `Match`, `MatchPlayer` foundations | No class match listing API; match currently lacks class/quiz/scheduling association. |
| `GET /matches/{id}/results` | Persisted question/player/reward result entities | Authorized results API and score definition missing. |
| `GET /quizzes` | `Question` API returns JSON banks by grade/assignment | No titled quiz catalog; bank assignment number is not a documented quiz ID. |
| `GET /match-options` | `MatchType`: Ranked/Friendly/Private | No options API, duration policy, or `INDIVIDUAL` mode model. Type and mode must remain distinct. |
| `POST /classes/{id}/matches` | Match persistence only | Class/quiz configuration, scheduling, lifecycle API and trusted game-server integration missing. Backend record creation alone cannot establish a live Mirror match. |
| `GET /admin/dashboard` | Community/student-license/teacher/class entities | Community dashboard and date-range gameplay metrics missing. |
| `GET /admin/teachers/activity` | Teacher class assignments | Activity aggregation and date-range listing missing. |
| `GET /admin/reports/export` | No matching API | Export schema, period policy and format missing. |
| `GET /admin/grades` | `Communities/grades` | Returns fixed grade IDs/values; counts and enrollment percentage missing. |
| `POST /admin/grades` | Fixed grade policy | Conflicts with fixed grades 7–12; no custom-grade creation endpoint. |
| `GET /admin/classes` | `Communities/classes?gradeId=...` | List exists; pagination/search/status/teacher/sort and enriched response missing. |
| `GET /admin/classes/{id}` | Class model | Detail, first-four-student preview, activity and score aggregation missing. |
| `POST /admin/classes` | `POST Communities/classes` | Name/grade creation exists; optional teacher assignment and requested response need extension. |
| `PUT /admin/classes/{id}` | `PATCH Communities/classes/{id}` | Name/grade update exists; requested partial semantics and teacher field differ. |
| `POST /admin/classes/{id}/assign-teacher` | `PUT Communities/teachers/{teacherUserId}/classes` | Existing operation replaces a teacher's complete class set. A class-level assignment must preserve unrelated assignments and resolve multiple-teacher semantics. |
| `POST /admin/classes/{id}/archive` | `DELETE Communities/classes/{id}` | Existing repeat-safe soft deletion sets `Deleted`; archival is not established as equivalent. |
| `GET /admin/teachers` | `Communities/teachers` | Paginated search/class/grade/status exists; title, code, grade display, student counts and sort missing. |
| `GET /admin/teachers/stats` | Teacher memberships/invitations | Stats API missing; activity vs account status must be defined. |
| `GET /admin/teachers/{id}` | Teacher list projection | Detail, title/code, last activity, license projection and latest-three audit events missing. |
| `PUT /admin/teachers/{id}` | No teacher-profile update route | Profile/title/grade mutation missing. Email changes affect shared account identity and require a safe policy. |
| `POST /admin/teachers/{id}/reassign-classes` | `PUT Communities/teachers/{teacherUserId}/classes` | Existing replacement capability is reusable; routes/shape differ. |
| `POST /admin/teachers/{id}/message` | Email service exists | Authorized teacher-message API missing; email vs internal notification delivery unspecified. |
| `POST /admin/teachers/{id}/deactivate` | `DELETE Communities/teachers/{teacherUserId}` | Existing removal sets membership `Removed`, releases a teacher seat and revokes invitations. Reversible inactivity/deactivation is not the same operation. |
| `GET /admin/teachers/export` | No matching API | Tenant-scoped export missing. |
| `GET /admin/invitations` | `TeacherInvitation` persisted | Authorized pending/expired list API missing. Accepted/revoked records need explicit exclusion policy. |
| `POST /admin/invitations` | `POST Communities/teachers/invite` | Existing invitation issuance and delivery are reusable; response is teacher-oriented. Default expiry is already seven days. |
| `POST /admin/invitations/{id}/resend` | Invitation service issuance logic | ID-based resend endpoint missing; preserve cooldown/token invalidation and membership constraints. |
| `DELETE /admin/invitations/{id}` | Membership-level invitation revocation service | Invitation-specific cancellation API missing. Seat/pending-membership consequences need a defined policy. |

## Confirmed data-model limitations

- `ListGradesQueryHandler` requires exactly the six values 7–12. Legacy custom rows may remain stored with a null numeric value but are not the supported current grade catalog.
- `ClassStatus` is `Active`/`Deleted`; `NO_TEACHER` can potentially be derived from assignments, but `ARCHIVED` is not an existing persisted status.
- `CommunityUserStatus` is `Active`/`Pending`/`Removed`. It does not implement seven-day inactivity. A license being active does not prove recent gameplay.
- Class assignments are plural (`TeacherClassAssignment`); the frontend represents one teacher per class. A teacher can also span multiple grades, while the requested teacher response has a single grade.
- Teacher title, teacher code, general recent-activity events, notifications, and titled quiz catalog are absent from inspected domain models.
- `GetStudentDetailQueryHandler` returns `new CommunityStudentAnalyticsResponse()` without calculating analytics. Zero/default fields are placeholders, not verified student activity statistics.
- `Match` has optional community ownership but no class ID, quiz ID, display title, scheduled time or duration setting. Existing status values are `Created`, `Started`, `Completed`, `Cancelled`.
- Match-player data contains correct/wrong answers and timing totals. A requested percentage score needs an agreed denominator and aggregation policy. Never accept frontend-submitted competitive results as authoritative facts.
- Unity's current handoff records match completion/history submission as future integration work. Historical reports require a trusted game-server ingestion path and actual persisted observations; endpoints alone do not populate activity data.

## Open decisions for future Spec Kit work

1. Establish single/multiple-teacher class assignment and teacher grade-display behavior; choose reversible deactivation vs existing membership removal.
2. Define activity events, teacher inactivity, score aggregation and historical class attribution. Seven-day inactivity and Monday–Sunday UTC calendar-week comparisons remain proposals.
3. Define message delivery, invitation cancellation/seat behavior, and profile-email mutation policy before those writes are implemented.
4. When match creation is taken up later, define quiz selection, modes, durations, scheduling, and backend-session versus Unity/Mirror orchestration.
5. When exports are taken up later, define export columns and format.

## Suggested implementation order when resumed

1. Create Spec Kit artifacts with the confirmed exclusions and deferrals above; resolve remaining business rules and document frontend mappings while preserving current APIs and community isolation.
2. Class/teacher/student read projections and scoped class management; use existing repositories and CQRS slices.
3. Invitation management and teacher profile/status changes with integrity tests.
4. Dashboard/search/notification read slices on agreed persisted facts, excluding enrollment percentage, term progress, and exports. Establish trusted result/activity ingestion where required for accurate metrics.
5. Match creation/start-now and exports only after the user explicitly resumes those deferred features.

Each implemented API slice must include its own `specs/<feature>/api.md`, `frontend.md`, relevant migration where needed, focused authorization/integrity tests, and solution build verification. No production code, settings, database or deployment was changed by this assessment.
