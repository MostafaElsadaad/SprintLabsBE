# Change inventory

Date: 2026-10-03. Backend branch: codex/021-dashboard-backend.
No new packages/project references. No Match/Question schema changes. Existing settings edits preserved.

- .specify/feature.json
- AGENTS.md
- API/Controllers/CommunityDashboardController.cs
- Application/Features/CommunityDashboard/AssignDashboardTeacher/AssignDashboardTeacherCommand.cs
- Application/Features/CommunityDashboard/AssignDashboardTeacher/AssignDashboardTeacherCommandHandler.cs
- Application/Features/CommunityDashboard/AssignDashboardTeacher/AssignDashboardTeacherRequest.cs
- Application/Features/CommunityDashboard/CancelDashboardInvitation/CancelDashboardInvitationCommand.cs
- Application/Features/CommunityDashboard/CancelDashboardInvitation/CancelDashboardInvitationCommandHandler.cs
- Application/Features/CommunityDashboard/Common/ActivityView.cs
- Application/Features/CommunityDashboard/Common/ClassDetail.cs
- Application/Features/CommunityDashboard/Common/ClassView.cs
- Application/Features/CommunityDashboard/Common/DashboardAuthorization.cs
- Application/Features/CommunityDashboard/Common/DashboardPaging.cs
- Application/Features/CommunityDashboard/Common/DashboardPeriod.cs
- Application/Features/CommunityDashboard/Common/DashboardProjection.cs
- Application/Features/CommunityDashboard/Common/DashboardScope.cs
- Application/Features/CommunityDashboard/Common/GradeSummary.cs
- Application/Features/CommunityDashboard/Common/GradeView.cs
- Application/Features/CommunityDashboard/Common/StudentView.cs
- Application/Features/CommunityDashboard/Common/TeacherSummary.cs
- Application/Features/CommunityDashboard/Common/TeacherView.cs
- Application/Features/CommunityDashboard/GetDashboardClass/GetDashboardClassQuery.cs
- Application/Features/CommunityDashboard/GetDashboardClass/GetDashboardClassQueryHandler.cs
- Application/Features/CommunityDashboard/GetDashboardTeacher/GetDashboardTeacherQuery.cs
- Application/Features/CommunityDashboard/GetDashboardTeacher/GetDashboardTeacherQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardClasses/ListDashboardClassesQuery.cs
- Application/Features/CommunityDashboard/ListDashboardClasses/ListDashboardClassesQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardGrades/ListDashboardGradesQuery.cs
- Application/Features/CommunityDashboard/ListDashboardGrades/ListDashboardGradesQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardInvitations/InvitationView.cs
- Application/Features/CommunityDashboard/ListDashboardInvitations/ListDashboardInvitationsQuery.cs
- Application/Features/CommunityDashboard/ListDashboardInvitations/ListDashboardInvitationsQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardNotifications/ListDashboardNotificationsQuery.cs
- Application/Features/CommunityDashboard/ListDashboardNotifications/ListDashboardNotificationsQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardNotifications/NotificationsResponse.cs
- Application/Features/CommunityDashboard/ListDashboardNotifications/NotificationView.cs
- Application/Features/CommunityDashboard/ListDashboardStudents/ListDashboardStudentsQuery.cs
- Application/Features/CommunityDashboard/ListDashboardStudents/ListDashboardStudentsQueryHandler.cs
- Application/Features/CommunityDashboard/ListDashboardTeachers/ListDashboardTeachersQuery.cs
- Application/Features/CommunityDashboard/ListDashboardTeachers/ListDashboardTeachersQueryHandler.cs
- Application/Features/CommunityDashboard/MessageDashboardTeacher/MessageDashboardTeacherCommand.cs
- Application/Features/CommunityDashboard/MessageDashboardTeacher/MessageDashboardTeacherCommandHandler.cs
- Application/Features/CommunityDashboard/MessageDashboardTeacher/MessageDashboardTeacherRequest.cs
- Application/Features/CommunityDashboard/ResendDashboardInvitation/ResendDashboardInvitationCommand.cs
- Application/Features/CommunityDashboard/ResendDashboardInvitation/ResendDashboardInvitationCommandHandler.cs
- Application/Features/CommunityDashboard/SearchDashboard/SearchDashboardQuery.cs
- Application/Features/CommunityDashboard/SearchDashboard/SearchDashboardQueryHandler.cs
- Application/Features/CommunityDashboard/SearchDashboard/SearchItem.cs
- Application/Features/CommunityDashboard/StaffDashboard/StaffDashboardQuery.cs
- Application/Features/CommunityDashboard/StaffDashboard/StaffDashboardQueryHandler.cs
- Application/Features/CommunityDashboard/StaffDashboard/StaffDashboardResponse.cs
- Application/Features/CommunityDashboard/StaffIdentity/StaffIdentityQuery.cs
- Application/Features/CommunityDashboard/StaffIdentity/StaffIdentityQueryHandler.cs
- Application/Features/CommunityDashboard/StaffIdentity/StaffIdentityResponse.cs
- Application/Features/CommunityDashboard/TeacherActivity/TeacherActivityQuery.cs
- Application/Features/CommunityDashboard/TeacherActivity/TeacherActivityQueryHandler.cs
- Application/Features/CommunityDashboard/TeacherActivity/TeacherActivityResponse.cs
- Application/Features/CommunityDashboard/TeacherStats/TeacherStatsQuery.cs
- Application/Features/CommunityDashboard/TeacherStats/TeacherStatsQueryHandler.cs
- Application/Features/CommunityDashboard/TeacherStats/TeacherStatsResponse.cs
- Application/Features/CommunityDashboard/UpdateDashboardTeacher/UpdateDashboardTeacherCommand.cs
- Application/Features/CommunityDashboard/UpdateDashboardTeacher/UpdateDashboardTeacherCommandHandler.cs
- Application/Features/CommunityDashboard/UpdateDashboardTeacher/UpdateDashboardTeacherRequest.cs
- Application/Features/CommunityDashboard/WriteDashboardClass/WriteDashboardClassCommand.cs
- Application/Features/CommunityDashboard/WriteDashboardClass/WriteDashboardClassCommandHandler.cs
- Application/ServiceConfig.cs
- docs/features/frontend-dashboard-gap-assessment.md
- Domain/Models/Community.cs
- Domain/Models/Notification.cs
- Domain/Models/StaffActivity.cs
- Domain/Services/ITeacherClassAssignmentService.cs
- Domain/Services/ITeacherInvitationService.cs
- Domain/Services/ITeacherProfileService.cs
- Infrastructure/DataAccess/ApplicationDbContext.cs
- Infrastructure/Migrations/20261002235407_CommunityDashboardManagement.cs
- Infrastructure/Migrations/20261002235407_CommunityDashboardManagement.Designer.cs
- Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs
- Infrastructure/ServiceConfig.cs
- Infrastructure/Services/TeacherClassAssignmentService.cs
- Infrastructure/Services/TeacherInvitationService.cs
- Infrastructure/Services/TeacherProfileService.cs
- Shared/Requests/DashboardClassRequest.cs
- specs/021-dashboard-backend/api.md
- specs/021-dashboard-backend/checklists/requirements.md
- specs/021-dashboard-backend/contracts/dashboard-api.md
- specs/021-dashboard-backend/data-model.md
- specs/021-dashboard-backend/frontend.md
- specs/021-dashboard-backend/plan.md
- specs/021-dashboard-backend/quickstart.md
- specs/021-dashboard-backend/research.md
- specs/021-dashboard-backend/spec.md
- specs/021-dashboard-backend/tasks.md
- SprintLabs.Tests/Features/CommunityDashboard/DashboardApiTests.cs
- SprintLabs.Tests/Features/CommunityDashboard/DashboardFixture.cs
- SprintLabs.Tests/Features/CommunityDashboard/DashboardInvitationTests.cs
- SprintLabs.Tests/Features/CommunityDashboard/DashboardManagementTests.cs
- SprintLabs.Tests/Features/CommunityDashboard/DashboardReadTests.cs

Cross-repository documentation update: Unity PROJECT_HANDOFF.md. No Unity assets, metadata or runtime code changed.

## Frontend contract correction — 2026-10-03
- API/Controllers/CommunitiesController.cs: existing class/grade/teacher/student operations now return requested public projections, with optional teacher assignment and frontend pagination.
- API/Controllers/CommunitiesController.Dashboard.cs: ten missing operations colocated with the existing routes.
- API/Controllers/CommunityDashboardController.cs: reduced to me/dashboard/search/notifications; no duplicate roster/management aliases.
- Application/Features/CommunityDashboard/Common/DashboardResponseMapper.cs: explicit minimal frontend field projections and user-requested numeric zero placeholders.
- Application/Features/CommunityDashboard/Common/ClassDetail.cs and GetDashboardClass handler: role-specific detail mapping; teacher sees own name/assigned class.
- StaffIdentity handler: assigned grade labels for the requested title.
- Student/teacher list queries/handlers: preserve grade filters; optional class filtering on existing students route stays assigned-class scoped.
- Invitation/notification queries/handlers/responses: plain arrays, matching requested contracts.
- Removed unused added archive/deactivate/read wrapper slices.
- SprintLabs.Tests/Features/CommunityDashboard/: corrected integration contracts and lifecycle/list assertions; four additional HTTP cases.
- SprintLabs.Tests/Features/CurrentCommunityResolution/CommunitiesControllerRouteContractTests.cs: updated existing route dispatch assertion.
- Spec/design/tasks/API/frontend/research/quickstart/contracts and baseline assessment updated; Unity PROJECT_HANDOFF.md updated.
Validation: build passes; 54 focused checks pass; full suite 416 passed/2 skipped. No new schema/migration, config or Unity production-code change in this correction.

## Seeded Postman runner — 2026-10-03
Added postman/SprintLabs.Dashboard.postman_collection.json, postman/SprintLabs.DemoLocal.postman_environment.json and postman/README.md. The collection has 87 requests (15 SMTP cases optional), auto-discovery, assertions and cleanup, with no actual credentials/token values in the artifacts. Updated API/frontend/quickstart links and task completion. Official schema structure, 173 script compilations, setup/escaping/dependencies/optional guards validated offline. No API code, database or email was changed/executed for this artifact.
