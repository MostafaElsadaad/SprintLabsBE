# Implementation Plan: Community dashboard backend
**Branch**: `codex/021-dashboard-backend` | **Date**: 2026-10-03 | **Spec**: [spec.md](spec.md)
## Summary
Implement community staff dashboard/management contracts from the frontend request. Per the 2026-10-03 correction, enrich existing Communities routes instead of duplicate dashboard routes, match frontend payload fields inside BaseResponse, and use zero placeholders for deferred numeric gameplay metrics. Preserve route/version/identity conventions and owner plural teachers. Game results, quiz/question schema, match creation/options/scheduling/start-now, exports, grade creation and progress percentages remain deferred/excluded.
## Technical Context
**Language/Version**: C# 12 / .NET 8.
**Primary Dependencies**: existing ASP.NET Core, MediatR, EF Core 8, Identity, Pomelo MySQL; no new packages/references.
**Storage**: MySQL; add recipient notifications, community teacher title and observed management activity.
**Testing**: xUnit/Moq/EF InMemory, real context model/migration verification and solution regression tests.
**Target Platform**: Existing backend.
**Project Type**: Layered web API.
**Performance Goals**: Bounded pages (1–100); stable sorting and community-filtered queries.
**Constraints**: Existing response envelope and numeric identities unchanged; frontend page/row fields replace duplicated roster contracts; no external environment/data/deployment mutation.
**Scale/Scope**: One community per staff context; four independently validated stories.
## Constitution Check
Passed before and after design: vertical slices, existing repositories/CQRS/DI, explicit community and assigned-class isolation, no packages or dependency changes, no speculative game ingestion/quiz schema, migration/test/docs included.
## Project Structure
- Domain/Models: Notification, StaffActivity; CommunityUser.TeacherTitle.
- Domain/Services: existing teacher invitation/class assignment services gain atomic management operations; teacher profile contract is an Identity boundary.
- Infrastructure: implementations, context mappings and generated migration; no Application dependency.
- Application/Features/CommunityDashboard: Common authorization/projections; one query/command and handler per action.
- API/Controllers/CommunityDashboardController.cs: staff read projections; management additions remain under CommunitiesController.
- SprintLabs.Tests/Features/CommunityDashboard: ownership, aggregation, validation and lifecycle tests.
- specs/021-dashboard-backend: specification, research, data model, contracts, tasks, API/frontend guide, quickstart.
## Design
Reuse IBaseRepository<T> for normal data reads/writes. Shared DashboardAuthorization resolves active caller/community and owner role or assigned class. Shared DashboardProjection handles complex reused class/teacher roster projections only. Thin MediatR handlers each own one endpoint/action. DashboardResponseMapper emits only requested public fields from internal authorization/filter projections. Existing Communities class/grade/teacher/student routes are enhanced; missing actions join that controller. CommunityDashboard contains only shared identity/dashboard/search/notification reads. Archive/remove/reassign/invite reuse existing public routes without aliases.
New per-class assignment runs in existing Infrastructure teacher assignment service transaction; optional teacher on class creation/update is handled atomically there rather than chaining two commits.
Notifications are recipient isolated and community scoped. Owner message stores notification and observed activity atomically. No outbound teacher message delivery.
Invitation resend/revoke lock the existing target account in a serializable transaction and reject superseded invitations; cancellation revokes pending credentials and releases one seat exactly once. Reuse issuance email workflow; never list token hashes.
Teacher edits use UserManager in Infrastructure and validate name/title. Login email changes return conflict pending a verified email-change workflow; derived grades cannot be independently mutated.
Dashboard counts come from current non-revoked licenses, active classes and memberships. Internal gameplay/session/score projections can retain unavailable values; the public frontend payload uses zero numeric placeholders and missing-date nulls, with no added availability flags.
