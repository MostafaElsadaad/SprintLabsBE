# Tasks: Community dashboard backend
## Phase 1: Setup
- [x] T001 Inspect existing routes/services/ownership and baseline build in docs/features/frontend-dashboard-gap-assessment.md.
- [x] T002 Create spec/design/checklist artifacts in specs/021-dashboard-backend/.
## Phase 2: Foundation
- [x] T003 Add notification/activity/title persistence and generated migration in Domain/Models/ and Infrastructure/DataAccess/ApplicationDbContext.cs.
- [x] T004 Add scoped authorization and reusable roster projections in Application/Features/CommunityDashboard/Common/.
## Phase 3: US1 — Scoped roster views
- [x] T005 [US1] Add class list/detail/student/grade read queries and DTOs in Application/Features/CommunityDashboard/.
- [x] T006 [US1] Add ownership/pagination/filter tests in SprintLabs.Tests/Features/CommunityDashboard/.
## Phase 4: US2 — Management
- [x] T007 [US2] Add teacher list/detail/stats/activity in Application/Features/CommunityDashboard/.
- [x] T008 [US2] Add atomic class mutation/assignment service operations in Infrastructure/Services/TeacherClassAssignmentService.cs.
- [x] T009 [US2] Add Identity-safe profile editing service and command in Infrastructure/Services/ and Application/Features/CommunityDashboard/UpdateDashboardTeacher/.
- [x] T010 [US2] Test profile/class management integrity in SprintLabs.Tests/Features/CommunityDashboard/.
## Phase 5: US3 — Invitations and messages
- [x] T011 [US3] Add list/resend/cancel invitation slices and atomic service extensions in Infrastructure/Services/TeacherInvitationService.cs.
- [x] T012 [US3] Add internal teacher message and isolated notification reads in Application/Features/CommunityDashboard/.
- [x] T013 [US3] Test obsolete invitation/retry/seat and notification isolation in SprintLabs.Tests/Features/CommunityDashboard/.
## Phase 6: US4 — Identity/dashboard/search
- [x] T014 [US4] Add staff identity/dashboard/search query slices in Application/Features/CommunityDashboard/.
- [x] T015 [US4] Expose thin versioned endpoints in API/Controllers/CommunityDashboardController.cs.
- [x] T016 [US4] Verify coverage/empty-dashboard/search tests in SprintLabs.Tests/Features/CommunityDashboard/.
## Final Phase: Validation
- [x] T017 Document every API and frontend mapping in specs/021-dashboard-backend/api.md and frontend.md.
- [x] T018 Run solution build/tests, migration script/model checks and diff review; record results in specs/021-dashboard-backend/quickstart.md.
- [x] T019 Update assessment scope and Unity PROJECT_HANDOFF.md with actual completed backend state.
## Contract correction — 2026-10-03
- [x] T020 Align public response fields to the frontend Markdown, with separate teacher/admin dashboards and zero numeric gameplay placeholders. Preserve owner plural teachers and teacher assigned-class scope.
- [x] T021 Consolidate duplicated class/grade/teacher/student operations into existing Communities routes; remove redundant dashboard aliases and notification-read action absent from the request.
- [x] T022 Add HTTP contract/route/ownership tests, run build and regressions, and update API/frontend/spec/handoff documentation.

## Dependencies
T001–T004 precede all stories; US1 → US2 → US3 → US4 → validation. Work sequentially within shared files.
## Parallel opportunities
Read-only documentation/test-scenario review is independent of implementation; production mutations run sequentially.
## Implementation strategy
Deliver and validate roster reads first, then management, then invitations/notifications, finally dashboard projections. No game/question/result integration or export work.

## Postman test artifact — 2026-10-03
- [x] T023 Provide a seeded-account collection, blank-secret local environment and run guide; validate format/scripts/dependencies/cleanup offline without changing API code or calling a live backend.
