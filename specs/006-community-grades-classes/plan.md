# Implementation Plan: Community Grades and Classes

**Branch**: `006-community-grades-classes` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-community-grades-classes/spec.md`

## Summary

Add community-owned Grade and Class management for active Owners and Teachers. The implementation will add `Grade` and `Class` entities, configure EF Core relationships and indexes, generate a migration, and expose member-facing community endpoints for creating/listing grades, creating/listing/updating classes, and soft deleting classes. All endpoints will reuse the existing community role service with `HasCommunityRole(userId, communityId, Owner, Teacher)`, return DTOs only, and exclude deleted classes from class lists and grade class counts. No student assignment, student licenses, analytics, imports, payments, dashboard, or grade update/delete workflow is planned.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>`, `ICommunityAccessService`, `IUserService`, `BaseResponse`, and `GenericException`

**Storage**: MySQL via EF Core; new `Grades` and `Classes` tables with relationships to existing `Communities`

**Testing**: Focused handler and EF InMemory tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` where the existing test environment permits

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Grade and class list operations remain scoped to one community; grade class counts are computed only for requested community grades and exclude deleted classes

**Constraints**: Follow existing controller/CQRS/DTO/BaseResponse/error patterns; use generic repositories; use existing role checks; do not introduce a permissions framework, authentication rewrite, custom repository, unrelated refactor, or future student/analytics/payment/dashboard scope

**Scale/Scope**: Six member-facing versioned endpoints, two new entities, one status enum for class soft delete, EF configuration, one migration, focused tests, API contract, quickstart, and frontend-facing documentation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers schema, entities, API slices, authorization, tests, contracts, docs, and validation.
- **Existing architecture wins**: PASS. The design uses current controllers, MediatR, generic repositories, EF Core, BaseResponse, GenericException, and the existing community role service.
- **SaaS data isolation**: PASS. Every operation is scoped by route `communityId`, authenticated user id, and Active Owner/Teacher membership. Grade/class ids must belong to the same route community.
- **JSON where flexibility matters**: PASS. No question JSON or JSON persistence behavior changes.
- **Minimum useful implementation**: PASS. Only grade creation/listing and class create/list/update/soft-delete are designed.
- **Quality gates**: PASS. Implementation requires migration review, focused authorization/tenant-isolation tests, solution build, and tests where feasible.
- **Documentation is executable context**: PASS. Planning artifacts, API contract, quickstart, and frontend-facing docs remain under this feature folder.

## Project Structure

### Documentation (this feature)

```text
specs/006-community-grades-classes/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- grades-classes-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

`tasks.md` is generated later by `speckit-tasks`.

### Source Code (repository root)

```text
API/
`-- Controllers/
    `-- CommunitiesController.cs

Application/
`-- Features/
    `-- Communities/
        `-- GradesClasses/
            |-- Common/
            |   |-- GradeResponse.cs
            |   `-- ClassResponse.cs
            |-- CreateGrade/
            |   |-- CreateGradeRequest.cs
            |   |-- CreateGradeCommand.cs
            |   `-- CreateGradeCommandHandler.cs
            |-- ListGrades/
            |   |-- ListGradesQuery.cs
            |   `-- ListGradesQueryHandler.cs
            |-- CreateClass/
            |   |-- CreateClassRequest.cs
            |   |-- CreateClassCommand.cs
            |   `-- CreateClassCommandHandler.cs
            |-- ListClasses/
            |   |-- ListClassesQuery.cs
            |   `-- ListClassesQueryHandler.cs
            |-- UpdateClass/
            |   |-- UpdateClassRequest.cs
            |   |-- UpdateClassCommand.cs
            |   `-- UpdateClassCommandHandler.cs
            `-- DeleteClass/
                |-- DeleteClassCommand.cs
                `-- DeleteClassCommandHandler.cs

Domain/
|-- Enums/
|   `-- CommunityEnums.cs
|-- Models/
|   `-- Community.cs
|-- Repositories/
|   `-- IBaseRepository.cs
`-- Services/
    |-- ICommunityAccessService.cs
    `-- IUserService.cs

Infrastructure/
|-- DataAccess/
|   `-- ApplicationDbContext.cs
|-- Migrations/
|   `-- <timestamp>_CommunityGradesAndClasses.cs
`-- Repositories/
    `-- BaseRepository.cs

SprintLabs.Tests/
`-- Features/
    `-- CommunityGradesClasses/
        |-- CreateGradeCommandHandlerTests.cs
        |-- ListGradesQueryHandlerTests.cs
        |-- CreateClassCommandHandlerTests.cs
        |-- ListClassesQueryHandlerTests.cs
        |-- UpdateClassCommandHandlerTests.cs
        `-- DeleteClassCommandHandlerTests.cs
```

**Structure Decision**: Keep grades/classes under the existing member-facing `Communities` feature and extend the existing `CommunitiesController` route tree. Keep authorization inside handlers through `ICommunityAccessService.HasCommunityRole` and persistence through `IBaseRepository<T>`.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Add `Grade` and `Class` entities to `Domain/Models/Community.cs` to match the current colocated community entity style.
- Add `ClassStatus` to `Domain/Enums/CommunityEnums.cs` with `Active` and `Deleted`, matching the project's status-enum lifecycle pattern.
- Configure relationships in `ApplicationDbContext`: Community 1-many Grades, Community 1-many Classes, Grade 1-many Classes.
- Use one migration for `Grades` and `Classes`.
- Use `HasCommunityRole(userId, communityId, new[] { Owner, Teacher })` for all grade/class endpoints.
- Validate route community ownership for every grade id and class id.
- Use generic repositories only; no custom repositories or permissions framework.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/grades-classes-api.openapi.yaml](./contracts/grades-classes-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design has schema, CQRS/API, authorization, tests, docs, and validation scenarios.
- **Existing architecture wins**: PASS. No new architecture, custom repository, package, or auth framework is required.
- **SaaS data isolation**: PASS. Community ownership checks are explicit for route community, grades, and classes.
- **JSON where flexibility matters**: PASS. Not applicable to this feature.
- **Minimum useful implementation**: PASS. Grade update/delete, student assignment, analytics, dashboard, import/export, payment, and licenses remain out of scope.
- **Quality gates**: PASS. Quickstart includes migration, build, tests, and manual role/ownership validation.
- **Documentation is executable context**: PASS. API and validation contracts are documented beside the specification.

## Complexity Tracking

No constitution violations.
