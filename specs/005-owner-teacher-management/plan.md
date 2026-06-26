# Implementation Plan: Owner Teacher Management

**Branch**: `005-owner-teacher-management` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-owner-teacher-management/spec.md`

## Summary

Add owner-only teacher management endpoints for community teacher invitations, teacher listing, and soft removal, plus pending-teacher activation during the existing Google login flow. The implementation will reuse `CommunityUser` with `Role = Teacher` and statuses `Pending`, `Active`, and `Removed`; reuse `CommunityLicense.MaxTeachers` and `UsedTeachers` for seat limits; and reuse the existing community role check with `HasCommunityRole(userId, communityId, Owner)` for every owner-facing teacher-management endpoint. No new tables, migrations, authentication rewrite, email workflow, or teacher dashboard is planned.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>`, `ICommunityAccessService`, `IUserService`, `BaseResponse`, `GenericException`, and the existing Google login handler flow

**Storage**: Existing MySQL `Users`, `Communities`, `CommunityUsers`, and `CommunityLicenses` tables; no new tables or planned migration

**Testing**: Focused handler/service tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` where the existing test environment permits

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Each request performs bounded user, membership, license, and community lookups scoped to one community; login activation scans only pending teacher memberships matching the normalized login email

**Constraints**: Follow existing controller/CQRS/DTO/BaseResponse/error patterns; use generic repositories; use `HasCommunityRole(..., Owner)` for all teacher-management endpoints; extend Google login only for pending teacher activation; do not introduce a permissions framework, authentication rewrite, new packages, new tables, migrations unless proven required, or future scope

**Scale/Scope**: Three owner-facing versioned endpoints, one login activation touchpoint, shared teacher DTOs, focused tests, API contract, quickstart, and frontend-facing documentation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers owner endpoints, authorization, seat accounting, login activation, tests, contracts, and documentation.
- **Existing architecture wins**: PASS. The design uses current controllers, MediatR, generic repositories, access service, BaseResponse, GenericException, and Google login flow.
- **SaaS data isolation**: PASS. Every owner-facing operation is scoped by authenticated user id, target community id, and Active Owner membership. Teacher activation is scoped by normalized login email and Teacher/Pending membership status.
- **JSON where flexibility matters**: PASS. No question JSON or JSON persistence behavior changes.
- **Minimum useful implementation**: PASS. No email delivery, dashboard, student/grade/class/license expansion, new permission framework, custom repository, schema change, or new table.
- **Quality gates**: PASS. Implementation requires focused tests for owner authorization, seat accounting, duplicate prevention, soft removal, and login activation, plus solution build and tests where feasible.
- **Documentation is executable context**: PASS. Planning artifacts, API contract, quickstart, and future frontend-facing docs remain under this feature folder.

## Project Structure

### Documentation (this feature)

```text
specs/005-owner-teacher-management/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- teacher-management-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

`tasks.md` is generated later by `speckit-tasks`.

### Source Code (repository root)

```text
API/
`-- Controllers/
    `-- CommunityTeachersController.cs

Application/
`-- Features/
    `-- Communities/
        `-- Teachers/
            |-- Common/
            |   `-- TeacherResponse.cs
            |-- InviteTeacher/
            |   |-- InviteTeacherRequest.cs
            |   |-- InviteTeacherCommand.cs
            |   `-- InviteTeacherCommandHandler.cs
            |-- ListTeachers/
            |   |-- ListTeachersQuery.cs
            |   `-- ListTeachersQueryHandler.cs
            `-- RemoveTeacher/
                |-- RemoveTeacherCommand.cs
                `-- RemoveTeacherCommandHandler.cs

Domain/
|-- Enums/
|   `-- CommunityEnums.cs
|-- Models/
|   |-- CommunityUser.cs
|   |-- CommunityLicense.cs
|   `-- User.cs
|-- Repositories/
|   `-- IBaseRepository.cs
`-- Services/
    |-- ICommunityAccessService.cs
    `-- IUserService.cs

Infrastructure/
|-- Repositories/
|   `-- BaseRepository.cs
`-- Services/
    `-- Google login related existing service/handler

SprintLabs.Tests/
`-- Features/
    `-- OwnerTeacherManagement/
        |-- InviteTeacherCommandHandlerTests.cs
        |-- ListTeachersQueryHandlerTests.cs
        |-- RemoveTeacherCommandHandlerTests.cs
        `-- PendingTeacherActivationTests.cs
```

**Structure Decision**: Add a focused `Communities/Teachers` feature area under the existing member-facing `Communities` feature rather than platform-admin community management. Keep the controller thin, keep owner authorization in handlers through the existing access service, and keep persistence on `IBaseRepository<T>` with `AsQueryable()` for simple scoped lookups.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Use one member-facing teacher controller with routes under `/api/v1/Communities/{communityId}/teachers`.
- Require `HasCommunityRole(userId, communityId, Owner)` for invite, list, and remove.
- Store invitations as `CommunityUser` rows with `Role = Teacher` and `Status = Pending`.
- Count Pending and Active teacher memberships against `CommunityLicense.UsedTeachers`.
- Increment `UsedTeachers` only when a new counted seat is created or a Removed teacher is restored.
- Activate Pending Teacher memberships during Google login by normalized email without incrementing `UsedTeachers`.
- Soft-remove teachers by setting status to Removed and decrementing `UsedTeachers` only when the prior status was Pending or Active.
- Use generic repositories only; do not add a custom repository or migration.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/teacher-management-api.openapi.yaml](./contracts/teacher-management-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. Each endpoint and login activation path has a complete authorization-to-persistence design and validation path.
- **Existing architecture wins**: PASS. The design requires no package, repository, auth framework, or schema changes.
- **SaaS data isolation**: PASS. All owner actions are scoped to the authenticated user's Active Owner membership in the exact community.
- **JSON where flexibility matters**: PASS. Not applicable to this feature.
- **Minimum useful implementation**: PASS. Only owner teacher invite/list/remove and pending activation are designed.
- **Quality gates**: PASS. Quickstart includes build, test, and manual validation for role access, capacity, duplicate invite, removal, and login activation.
- **Documentation is executable context**: PASS. API and validation contracts are documented beside the specification.

## Complexity Tracking

No constitution violations.
