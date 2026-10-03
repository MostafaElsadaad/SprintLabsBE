# Community dashboard API
Verified implementation: 2026-10-03, branch `codex/021-dashboard-backend`.

## Conventions and scope
`D` means `/api/v1/CommunityDashboard`; `C` means `/api/v1/Communities`.
Bearer authentication is required. Successful calls return HTTP 200 with the existing envelope:
```json
{ "data": {}, "message": "Success", "statusCode": 200, "errorCode": 1 }
```
Response fields inside `data` follow the supplied frontend Markdown. Numeric IDs remain Int64, not the document's illustrative `c1`/`t1` strings. Pagination now matches the requested `{items,page,pageSize,total}`. Send `page` (default 1), `pageSize` (default 10, max 100); legacy `pageNumber` is also accepted. When both are supplied, `page` takes precedence.

Owner maps to COMMUNITY_ADMIN; Teacher maps to TEACHER. Caller identity comes from the bearer claim, never query/body IDs. Community is resolved from the caller's single current staff membership. Teachers can read only assigned active classes and their rosters. Owner management and teacher/invitation management return 403 for teachers. Platform-admin status alone does not grant community-owner access.

Deferred numeric gameplay metrics are **0 placeholders**, as explicitly requested; they do not represent measured game results. Missing dates and optional text remain null. Student activity remains UNKNOWN until game integration; activity filtering accepts only omitted/UNKNOWN. Grades remain predefined 7–12. No termProgress/enrollmentPercent, custom grades, match/results/quiz/question-schema work, start-now, scheduling or exports.

## Read endpoints
GET requests have no body.

| Endpoint | Role | Query | Success `data` |
| --- | --- | --- | --- |
| GET D/me | Staff | none | `{id,fullName,role,title,unreadNotificationsCount}`; teacher title is assigned grade names joined by comma, null if none; owner title null |
| GET D/dashboard | Staff | optional from,to | Separate teacher/admin shapes below, selected from caller role |
| GET D/search | Staff | q, max 100 chars | `[{id,type,name}]`, stable name ordering, max 50; owner class/student/teacher, teacher students only; empty q returns [] |
| GET D/notifications | Staff | none | `{notifications:[{id,title,body,createdAt,isRead}],unreadCount}`; all current recipient/community notifications, newest first |
| GET C/grades | Staff | none | `[{id,name,classesCount,studentsCount}]`; fixed grade IDs; counts follow visible active classes |
| GET C/classes | Staff | page,pageSize,search,gradeId,status,teacherId,sort | Page of owner class rows below; teacher sees assigned active classes only |
| GET C/classes/{classId} | Staff | none | Role-specific class detail below |
| GET C/students | Staff | classId,gradeId,page,pageSize,search,status,sort | Page of `{id,fullName,avgScore:0,sessionsCount:0,status:"UNKNOWN"}`; classId maps the requested `/classes/{id}/students` action |
| GET C/teachers | Owner | page,pageSize,search,classId,title,status,gradeId,sort | Page of teacher list rows below |
| GET C/teachers/{teacherId} | Owner | none | Teacher detail below |
| GET C/teachers/stats | Owner | none | `{total,active,inactive,pendingInvitations}` |
| GET C/teachers/activity | Owner | from,to,page,pageSize | Page of `{teacherId,fullName,classesCount,totalSessions:0,lastPlayedAt:null}` |
| GET C/invitations | Owner | none | `[{id,email,sentAt,expiresAt,status}]`; current unaccepted/unrevoked invitations of pending memberships; PENDING/EXPIRED |

Class status: ACTIVE/NO_TEACHER/ARCHIVED, default excludes archived. Class sort: `name:asc/desc`, `createdAt:asc/desc`, `studentsCount:asc/desc`. Teacher title TEACHER/LEAD_TEACHER, status ACTIVE/INACTIVE/PENDING, sort name/joinedAt. Student sort name. Teacher search matches name/email/code; student search matches name/email.

### Teacher dashboard
```json
{
  "classesCount": 2,
  "studentsCount": 8,
  "sessionsThisWeek": 0,
  "sessionsChangeVsLastWeekPercent": 0,
  "gameplayHoursThisWeek": 0,
  "avgHoursPerStudent": 0,
  "classes": [
    { "id": 1, "name": "Class 7A", "grade": 7, "studentsCount": 4, "nextSession": null },
    { "id": 2, "name": "Class 7B", "grade": 7, "studentsCount": 4, "nextSession": null }
  ]
}
```
Only the teacher's assigned active classes are returned. No role, admin counters, availability flags, teacher lists or creation/status fields are added to dashboard cards. termProgress is excluded at the user's earlier request; nextSession is null because scheduling is deferred.

### Owner dashboard
`{totalStudents,newStudentsThisWeek,activeClasses,gradesCount,teachersCount,pendingInvitationsCount,gameplayHours:0,gameplayHoursChangeVsPreviousMonthPercent:0}`.
Roster totals count non-revoked licenses in active classes, including pending licenses. New students uses license creation in the Monday–Sunday UTC week. gradesCount is six. teachersCount includes active/pending memberships, excluding removed. Pending invitation count includes valid unaccepted/unrevoked current invitations only.
`from/to` validate an inclusive range of at most 367 dates; roster counts are current snapshots. Game period comparisons remain placeholders. Teacher activity accepts the same date validation; no game observations exist yet.

### Classes
Owner list row: `{id,name,grade:{id,name},teachers:[{id,fullName}],studentsCount,status}`.
Owner detail: `{id,name,grade:{id,name},status,teachers:[{id,fullName}],studentsCount,avgScore:0,createdAt,lastActiveAt:null,studentsPreview:[{id,fullName}]}`.
The user explicitly retained **plural teachers for owners**; all active assigned teachers are returned. NO_TEACHER means no active teacher assignment. studentsPreview contains the first four students in name/ID order.
Teacher detail: `{id,name,grade:7,teacherName,createdAt,studentsCount,avgScore:0,inactiveStudentsCount:0}`. teacherName is the calling assigned teacher's name. Teacher dashboard cards contain no teacher roster.
Archive uses existing Deleted status. No student accounts are deleted.

### Teachers
List row: `{id,fullName,title,teacherCode,grade,classes:[{id,name}],studentsCount,joinedAt,status}`.
Detail: `{id,fullName,teacherCode,title,status,email,grade,studentsCount,joinedAt,lastActiveAt,licenseStatus,classes:[{id,name,studentsCount}],recentActivity:[{label,at}]}`.
Grade is assigned grade names joined by comma; null when none. Pending teachers have grade null, classes [], studentsCount 0 (numeric-null override requested by the user). Multiple class/grade assignments remain stored. Code is deterministic `TCH-{UserId padded to at least 3 digits}`.
ACTIVE/PENDING/INACTIVE reflect membership, with INACTIVE meaning removed; no seven-day gameplay-inactivity inference. LicenseStatus is ASSIGNED for active/pending seats and NOT_ASSIGNED for removed membership. Stats total includes all retained memberships; active/inactive count their respective membership states.
RecentActivity is the latest three observed management events plus joined-community fallback. lastActiveAt is the last observed management event, null if none; it does not imply gameplay or login. Activity report totalSessions is 0 and lastPlayedAt is null. Internal coverage/audit fields are not returned.

### Student identifiers
The `id` in roster/search/preview rows is the student-license ID. It is distinct from account UserId and PlayerProfileId. The existing legacy `GET C/students/{playerProfileId}` still uses a player profile ID and its existing detail contract. Licensing administration remains under C/student-licenses, with existing response/status semantics. Neither route was replaced by a second dashboard route.

## Write endpoints
All owner-only. No alternative dashboard mutation aliases exist.

| Endpoint | Body | Success `data` / behavior |
| --- | --- | --- |
| POST C/classes | `{name,gradeId,teacherId:null}` | Owner class detail; name/grade required, optional active teacher assigned atomically |
| PATCH C/classes/{classId} | `{name?,gradeId?,teacherId?}` | Owner class detail; partial update, maps requested PUT to established PATCH |
| POST C/classes/{classId}/assign-teacher | `{teacherId}` | true; adds assignment and preserves other teachers/classes |
| DELETE C/classes/{classId} | none | Existing ClassResponse; repeat-safe archive using existing soft deletion, maps requested POST archive |
| PATCH C/teachers/{teacherId} | `{fullName?,title?}` | Teacher detail; name max 100, title TEACHER/LEAD_TEACHER; login email and derived grade read-only |
| PUT C/teachers/{teacherId}/classes | `{classIds:[1,2]}` | Existing TeacherResponse; replaces complete assignment set, [] clears all |
| POST C/teachers/{teacherId}/message | `{subject,body}` | Created notification ID; in-app only, subject max 200/body max 4000, both nonempty |
| DELETE C/teachers/{teacherId} | none | Existing TeacherResponse; removes community membership and releases seat, maps requested deactivate |
| POST C/teachers/invite | `{email,classIds:[]}` | Existing TeacherResponse; email required, classIds optional; maps requested invitation creation |
| POST C/invitations/{invitationId}/resend | none | true; uses existing invitation email flow, rotates token/ID and preserves assignments |
| DELETE C/invitations/{invitationId} | none | true; cancel current pending membership and release seat once; retries safe |

Name is trimmed/nonempty, max 100; active same-grade class names must be unique. Grade must belong to this community and predefined set. teacherId null/omitted preserves assignments; use full teacher reassignment to remove them. Foreign/removed/pending teachers cannot be newly assigned. Resend accepts current PENDING/EXPIRED; default cooldown 60 seconds, expiry seven days. Refresh invitations after resend because ID changes. Old IDs cannot cancel replacements or active/accepted teachers. No invitation token/hash is returned in management reads.
No notification mark-read HTTP action was requested; the extra action introduced in the first implementation was removed. Notifications expose their existing persisted isRead state without changing it on GET.

## Errors and compatibility
401 missing/malformed authentication; 403 wrong role/current-community context or suspended caller; 404 unknown/foreign/unassigned resource; 400 invalid page/range/filter/body or derived grade edit; 409 duplicate class, obsolete/accepted invitation, or changed login email; 429 invitation resend cooldown. Existing controlled error envelope remains unchanged.
Changed existing public responses: GET C/classes (array becomes requested page), GET C/teachers and GET C/students (frontend page/trimmed rows), GET C/grades (aggregate fields), POST/PATCH C/classes (optional teacher and owner detail). Existing consumers must switch to `response.data.items` for pages. C/student-licenses, C/students/{playerProfileId}, game GET /Users/me and invitation/reassignment/removal contracts remain intact.
CommunityDashboard now exposes four shared reads. Ten missing operations were added to Communities; six existing operations were enhanced. The former 23 dashboard routes are not retained as duplicate aliases.

## Automated seeded Postman runner
Import the collection and environment from [postman/README.md](postman/README.md), enter seedPassword locally, and run all folders in order. There are 87 definitions: 72 default requests and 15 optional SMTP/invitation lifecycle requests. The runner captures IDs/tokens automatically, validates contracts/permissions, restores the seeded teacher profile/assignments and archives its disposable class. Successful SMTP/teacher removal tests require the optional test mailbox; default permission/unknown-ID checks cover those routes without sending email. One notification, management activity and archived test class remain per run. The collection has been validated offline, not run against a live database.
