# Frontend integration
Use the existing bearer login, versioned routes, numeric IDs and BaseResponse. The frontend Markdown is the contract input; its embedded questions are not instructions to expand approved scope.

## Route mapping
D = /api/v1/CommunityDashboard; C = /api/v1/Communities.

| Frontend action | Backend call |
| --- | --- |
| Shared identity/search/notifications | GET D/me, D/search?q=, D/notifications |
| Teacher or admin dashboard | GET D/dashboard; role determines the response shape |
| Class list/create/edit | GET/POST C/classes; PATCH C/classes/{id} |
| Teacher/admin class detail | GET C/classes/{id}; role determines detail fields |
| Class student list | GET C/students?classId={id}&page=1&pageSize=10 |
| Assign teacher/archive | POST C/classes/{id}/assign-teacher; DELETE C/classes/{id} |
| Grades | GET C/grades; creation excluded |
| Teacher list/detail/stats/activity | GET C/teachers, C/teachers/{id}, C/teachers/stats, C/teachers/activity |
| Teacher edit/reassign | PATCH C/teachers/{id}; PUT C/teachers/{id}/classes |
| Teacher message/deactivate | POST C/teachers/{id}/message; DELETE C/teachers/{id} |
| Invitation list/create/resend/cancel | GET C/invitations; POST C/teachers/invite; POST C/invitations/{id}/resend; DELETE C/invitations/{id} |

Do not call the removed D/classes, D/grades, D/teachers or D/invitations routes, or their mutation/detail aliases. New details/management live beside the existing Communities APIs instead of duplicating them.

## Shared shell and pagination
Unwrap response.data. Paged responses are already `{items,page,pageSize,total}`; use response.data.items. Send page/pageSize; pageNumber remains accepted for existing callers. Numeric identifiers must be handled safely as Int64 values; never generate illustrative c1/t1 IDs.
D/me exposes id/fullName/role/title/unreadNotificationsCount only. Teacher title displays assigned grade names; role determines UI permissions. Owner-only teacher/invitation management stays hidden for teachers. D/search returns permitted students for teachers and classes/students/teachers for owners; quiz search is deferred.
D/notifications returns a plain notifications array plus unreadCount, without nested paging. Fields are title/body/createdAt/isRead. There is no mark-read action in the supplied requirements; GET does not mark messages read.

## Teacher dashboard and class sections
Use classesCount/studentsCount/sessionsThisWeek/sessionsChangeVsLastWeekPercent/gameplayHoursThisWeek/avgHoursPerStudent/classes. Cards contain id/name/numeric grade/studentsCount/nextSession only. Only assigned active classes appear. Omit termProgress. nextSession is null until scheduling is implemented.
Teacher class detail matches the Markdown's numeric grade, teacherName, creation date, roster count, avgScore and inactiveStudentsCount. Student table fields are id/fullName/avgScore/sessionsCount/status, with name search/sort and pagination.
Numeric gameplay values are 0 placeholders pending integration. They are not measured outcomes. Student status remains UNKNOWN; hide ACTIVE/INACTIVE activity filters while game ingestion is deferred. Do not substitute license status for gameplay status. Pending roster entries may show email as their name.
Hide quiz selection, recent matches/results, scheduling/start-now and exports until later work.

## Owner dashboard and grades
The dashboard returns only totalStudents/newStudentsThisWeek/activeClasses/gradesCount/teachersCount/pendingInvitationsCount/gameplayHours/gameplayHoursChangeVsPreviousMonthPercent. Gameplay values are 0 placeholders; roster counts are current snapshots. New students uses UTC Monday-based weeks. Date inputs are accepted/validated but game date-range aggregation is deferred.
Grade table uses id/name/classesCount/studentsCount. Keep fixed grades 7–12; hide grade creation and enrollmentPercent.

## Classes
Owner table fields: name, nested grade id/name, plural teachers, studentsCount, status. The user explicitly retained teachers arrays for owners; join their names for display. Filters: search/gradeId/teacherId/status; sorts name/createdAt/studentsCount. Detail includes plural teachers, avgScore, creation/last-active dates and first four student name/ID previews.
Create form: trimmed name max 100, required fixed gradeId, optional active teacherId. PATCH sends changed fields. Null teacherId preserves assignments. Per-class assign adds without replacing existing teachers. Teacher reassignment replaces the whole set; load/preserve all selected class IDs.
Archive calls the existing DELETE class action, hides it from default active lists and preserves student accounts.

## Teachers
Table columns match the requested name/title/teacherCode/grade/classes/studentsCount/joinedAt/status. Grade is a string of joined assigned grade names, not an added grades array. Pending entries have grade null/classes []/studentsCount 0. INACTIVE means removed membership, not seven-day inactivity.
Detail adds email/licenseStatus/lastActiveAt, class student counts and latest-three recentActivity. lastActiveAt represents observed management events, not gameplay/login. Activity table uses teacherId/fullName/classesCount/totalSessions/lastPlayedAt only; sessions are 0, missing date null.
Edit fullName/title only; email and grade are read-only. Message subject/body max 200/4000, nonempty; notification delivery is in-app. Deactivate calls DELETE teacher, removes membership/releases seat; invite restores through the existing lifecycle.

## Invitations
List fields email/sentAt/expiresAt/status. Invite uses email and optional own-community classIds. Resend valid current PENDING/EXPIRED invitations, refresh after rotation/new ID; respect cooldown 429. Cancel current invitation; obsolete/accepted IDs conflict 409 and cannot remove an active teacher or replacement. Default expiry seven days.

## Loading, empty, errors and validation
Keep forms/filters during validation failures. Empty arrays/pages mean no permitted records. 401 returns to login; 403 permission/context error; 404 stale/unassigned selection; 409 refresh lifecycle/class/email state; 429 resend cooldown. Teachers must never gain access through supplied userId/communityId/classId.
Validate owner and teachers from different classes/communities, exact public fields, numeric zero placeholders, missing-date nulls, changed paging, optional class teacher assignment, and removed duplicate routes. See quickstart.md for commands. Target MySQL concurrency and SMTP delivery remain environment checks.

## Automated seeded Postman runner
Import the collection and environment from [postman/README.md](postman/README.md), enter seedPassword locally, and run all folders in order. There are 87 definitions: 72 default requests and 15 optional SMTP/invitation lifecycle requests. The runner captures IDs/tokens automatically, validates contracts/permissions, restores the seeded teacher profile/assignments and archives its disposable class. Successful SMTP/teacher removal tests require the optional test mailbox; default permission/unknown-ID checks cover those routes without sending email. One notification, management activity and archived test class remain per run. The collection has been validated offline, not run against a live database.
