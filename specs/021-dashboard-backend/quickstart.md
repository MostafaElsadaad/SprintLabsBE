# Quickstart
Use a configured disposable Development/test database. API startup applies pending migrations to its configured database; this task did not start the API or apply a live migration.

```powershell
dotnet build SprintLabs.sln --no-restore -v quiet
dotnet test SprintLabs.sln --no-restore --filter 'FullyQualifiedName~CommunityDashboard|FullyQualifiedName~CommunitiesControllerRouteContractTests' -v quiet
dotnet test SprintLabs.sln --no-restore -v quiet
```

1. Restart your local API to load the corrected contracts. Use existing staff login with an owner/teacher having one active community membership. Never paste credentials into docs.
2. Teacher: GET `/api/v1/CommunityDashboard/dashboard`. Verify classesCount/studentsCount, four numeric gameplay placeholders 0, and classes containing only id/name/numeric grade/studentsCount/nextSession. Only assigned active classes appear; nextSession is null, termProgress omitted.
3. Owner: GET the same dashboard. Verify only the eight requested admin fields, with numeric gameplay placeholders 0.
4. GET `/api/v1/Communities/classes?page=1&pageSize=10`. Read `data.items`, `data.page`, `data.pageSize`, `data.total`; owner class rows retain plural teachers. GET `/api/v1/Communities/classes/{id}` for role-specific detail.
5. GET `/api/v1/Communities/students?classId={id}&page=1&pageSize=10`. Score/sessions are 0, gameplay status UNKNOWN. Teacher unassigned/foreign class IDs must fail without data disclosure.
6. GET `/api/v1/Communities/grades`, `/teachers`, `/teachers/{id}`, `/teachers/stats`, `/teachers/activity` and `/invitations` to validate trimmed frontend shapes; teacher management reads reject teachers with 403.
7. Owner POST/PATCH `/api/v1/Communities/classes` and `classes/{id}` using fixed grade IDs and optional teacherId; assign via `POST classes/{id}/assign-teacher`. DELETE class archives using the existing route. Preserve other assignments.
8. PATCH `/api/v1/Communities/teachers/{id}` edits fullName/title. Email remains read-only; changed login email conflicts 409. Reassign via existing PUT `teachers/{id}/classes`; deactivate via existing DELETE `teachers/{id}`.
9. Invite via existing POST `teachers/invite`, list invitations, resend after cooldown, refresh the new ID, and cancel current invitation twice; seats release once. Obsolete IDs cannot cancel replacements/active teachers. Use test mailbox for SMTP validation.
10. POST `Communities/teachers/{id}/message`; GET `CommunityDashboard/notifications` as the recipient. It returns a plain notifications array plus unreadCount. Other users cannot see that message. No extra mark-read route is exposed.
11. Verify old `/api/v1/CommunityDashboard/classes`, `/grades`, `/teachers`, `/invitations`, archive/deactivate/read aliases return 404. All roster/management operations now use Communities.

See api.md/frontend.md for complete bodies, permissions, status/filter rules and route mappings.

## Validation — 2026-10-03 contract correction
- Solution build passed, 0 errors (incremental final build also reports 0 warnings; baseline clean-build warnings remain unchanged).
- Focused dashboard/current-community controller contract run: 54 passed, 0 skipped.
- Full solution regression suite: 416 passed, 2 skipped, 418 total. The two existing MySQL concurrency tests skip because their database fixture is not configured.
- HTTP tests assert exact public field sets, numeric zero placeholders, date nulls, frontend paging, owner plural teachers, assigned-class privacy, existing create/update/archive calls and removed duplicate routes.
- This correction does not change the generated database migration/model, packages, project references, Unity runtime/assets/meta/assemblies, configuration credentials or deployed environments.
- MySQL DDL/locking/concurrency and actual invitation SMTP delivery remain target-environment checks. Game metrics/status/results, questions/quiz schema, scheduling/start-now and exports remain deferred; numeric zeros are placeholders.

## Automated seeded Postman runner
Import the collection and environment from [postman/README.md](postman/README.md), enter seedPassword locally, and run all folders in order. There are 87 definitions: 72 default requests and 15 optional SMTP/invitation lifecycle requests. The runner captures IDs/tokens automatically, validates contracts/permissions, restores the seeded teacher profile/assignments and archives its disposable class. Successful SMTP/teacher removal tests require the optional test mailbox; default permission/unknown-ID checks cover those routes without sending email. One notification, management activity and archived test class remain per run. The collection has been validated offline, not run against a live database.
