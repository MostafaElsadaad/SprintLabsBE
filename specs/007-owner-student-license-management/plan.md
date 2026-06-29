# Implementation Plan: Owner Student License Management

**Branch**: `007-owner-student-license-management` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-owner-student-license-management/spec.md`

## Summary

Add owner-managed student license allocation for communities. The implementation will add a `StudentLicense` entity and status enum, configure EF Core relationships and indexes, generate a `StudentLicenses` migration, and expose member-facing community endpoints for adding, listing, updating, and revoking student licenses. All endpoints will use the existing authenticated user claim, `ICommunityAccessService.HasCommunityRole(userId, communityId, Owner)`, `IBaseRepository<T>`, MediatR/CQRS slices, `BaseResponse`, and project `GenericException` style. Student activation, email delivery, bulk import, dashboards, analytics, payments, parent accounts, teacher management, and grade/class management remain out of scope.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>`, `ICommunityAccessService`, `IUserService`, `BaseResponse`, and `GenericException`

**Storage**: MySQL via EF Core; new `StudentLicenses` table related to existing `Communities`, `CommunityLicenses`, `Grades`, `Classes`, and optionally `Users`/`Players`

**Testing**: Focused handler tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` for quota, authorization, grade/class validation, duplicate email, email-change limits, and revocation behavior

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Student license list operations remain scoped to one community and can filter by indexed status, grade, class, and normalized email/search text without scanning unrelated communities

**Constraints**: Follow existing controller/CQRS/DTO/BaseResponse/error patterns; use generic repositories; use existing community role checks; do not introduce a permissions framework, authentication rewrite, custom repository, unrelated refactor, or future student activation/email/dashboard/import/payment scope

**Scale/Scope**: Four member-facing versioned endpoints, one new entity, one status enum, EF configuration, one migration, focused tests, API contract, quickstart, and frontend-facing docs during implementation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers schema, domain/application logic, API contract, tests, migration, and documentation.
- **Existing architecture wins**: PASS. The design uses current controllers, MediatR, generic repositories, EF Core, BaseResponse, GenericException, and the existing community role service.
- **SaaS data isolation**: PASS. Every operation is scoped by route `communityId`, authenticated user id, Active Owner membership, and same-community grade/class/license ownership checks.
- **JSON where flexibility matters**: PASS. No question JSON or flexible payload behavior changes.
- **Minimum useful implementation**: PASS. Student activation, email sending, dashboards, analytics, imports, payments, parent accounts, teacher management, and grade/class management are explicitly excluded.
- **Quality gates**: PASS. Implementation requires migration review, focused tests, solution build, and test run where feasible.
- **Documentation is executable context**: PASS. Planning artifacts, contract, quickstart, and future frontend-facing docs remain under this feature folder.

## Project Structure

### Documentation (this feature)

```text
specs/007-owner-student-license-management/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- student-licenses-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

`tasks.md` is generated later by `/speckit-tasks`.

### Source Code (repository root)

```text
API/
`-- Controllers/
    `-- CommunitiesController.cs

Application/
`-- Features/
    `-- Communities/
        `-- StudentLicenses/
            |-- Common/
            |   |-- StudentLicenseResponse.cs
            |   |-- StudentLicenseGradeResponse.cs
            |   |-- StudentLicenseClassResponse.cs
            |   `-- StudentLicenseAuthorization.cs
            |-- AddStudentLicense/
            |   |-- AddStudentLicenseRequest.cs
            |   |-- AddStudentLicenseCommand.cs
            |   `-- AddStudentLicenseCommandHandler.cs
            |-- ListStudentLicenses/
            |   |-- ListStudentLicensesQuery.cs
            |   `-- ListStudentLicensesQueryHandler.cs
            |-- UpdateStudentLicense/
            |   |-- UpdateStudentLicenseRequest.cs
            |   |-- UpdateStudentLicenseCommand.cs
            |   `-- UpdateStudentLicenseCommandHandler.cs
            `-- RevokeStudentLicense/
                |-- RevokeStudentLicenseCommand.cs
                `-- RevokeStudentLicenseCommandHandler.cs

Domain/
|-- Enums/
|   `-- CommunityEnums.cs
|-- Models/
|   |-- Community.cs
|   `-- StudentLicense.cs
|-- Repositories/
|   `-- IBaseRepository.cs
`-- Services/
    |-- ICommunityAccessService.cs
    `-- IUserService.cs

Infrastructure/
|-- DataAccess/
|   `-- ApplicationDbContext.cs
|-- Migrations/
|   `-- <timestamp>_OwnerStudentLicenseManagement.cs
`-- Repositories/
    `-- BaseRepository.cs

SprintLabs.Tests/
`-- Features/
    `-- OwnerStudentLicenseManagement/
        |-- AddStudentLicenseCommandHandlerTests.cs
        |-- ListStudentLicensesQueryHandlerTests.cs
        |-- UpdateStudentLicenseCommandHandlerTests.cs
        `-- RevokeStudentLicenseCommandHandlerTests.cs
```

**Structure Decision**: Keep endpoints under the existing member-facing `CommunitiesController` route tree. Keep application code under `Application/Features/Communities/StudentLicenses/{SubFeature}` to match the current feature-folder convention. Add `StudentLicense` as its own domain model file instead of expanding the already-colocated `Community.cs`; add only required navigation properties to `Community`. Use `IBaseRepository<T>.AsQueryable()` for simple checks and long-id lookups.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Add `StudentLicenseStatus` to `Domain/Enums/CommunityEnums.cs` with `Pending`, `Active`, and `Revoked`.
- Add `StudentLicense` as `Domain/Models/StudentLicense.cs`; add `Community.StudentLicenses` navigation only if useful for EF relationship configuration.
- Use one migration for `StudentLicenses`.
- Use `HasCommunityRole(userId, communityId, new[] { CommunityUserRole.Owner })` for all endpoints.
- Validate grade, class, class-grade relationship, and class active status inside handlers before mutations.
- Normalize email by trimming and lowercasing for duplicate checks and persistence.
- Use `CommunityLicense.UsedStudents` as the stored seat counter and mutate it transactionally with license create/revoke saves.
- Revoke active student access by setting the matching `CommunityUser` with `Role = Student` to `Removed`; do not delete the row or touch other memberships.
- Use generic repositories only; no custom repository or permissions framework.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/student-licenses-api.openapi.yaml](./contracts/student-licenses-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design includes schema, CQRS/API, authorization, tests, docs, and validation scenarios.
- **Existing architecture wins**: PASS. No new architecture, custom repository, package, or auth framework is required.
- **SaaS data isolation**: PASS. Community ownership checks are explicit for route community, grades, classes, student licenses, and student access cleanup.
- **JSON where flexibility matters**: PASS. Not applicable to this feature.
- **Minimum useful implementation**: PASS. Student activation, email sending, imports, dashboards, analytics, payments, parent accounts, teacher management, and grade/class management remain out of scope.
- **Quality gates**: PASS. Quickstart includes migration, build, tests, and manual role/quota/ownership validation.
- **Documentation is executable context**: PASS. API and validation contracts are documented beside the specification.

## Complexity Tracking

No constitution violations.
