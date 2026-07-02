# Implementation Plan: Community Student Viewing

**Branch**: `009-community-student-viewing` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-community-student-viewing/spec.md`

## Summary

Add read-only student viewing endpoints for community Owners and Teachers. The implementation will extend the existing `CommunitiesController`, add CQRS query slices under `Application/Features/Communities/Students/`, use `StudentLicense` as the roster source of truth, validate grade/class filters against the route community, return project DTOs wrapped in `BaseResponse`, and use the existing `PagedRequest`/`PagedResponse` pagination types. No schema change, migration, new permissions framework, authentication rewrite, or student license mutation is planned.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core API controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>` / `BaseRepository<T>`, `ICommunityAccessService`, `IUserService`, `BaseResponse`, `GenericException`, `PagedRequest`, and `PagedResponse`

**Storage**: Existing MySQL tables through EF Core: `StudentLicenses`, `Users`, `Players`, `Grades`, `Classes`, and `CommunityUsers`. No new tables or migration required.

**Testing**: Focused handler tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` when implementing behavior and access rules

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET backend API monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Student list queries should remain scoped by `CommunityId` before filters/search, page results before returning, and avoid loading unrelated communities' data.

**Constraints**: Follow existing SprintLabs CQRS/MediatR/controller/DTO/BaseResponse/GenericException/repository patterns; use `HasCommunityRole(userId, communityId, Owner, Teacher)` for both endpoints; use existing `PagedRequest` and `PagedResponse`; do not add a pagination model; do not add tables, migrations, packages, permissions framework, or authentication changes; do not mutate student licenses; analytics object is placeholder only.

**Scale/Scope**: Two read-only endpoints under the existing Communities API: paginated/filterable community student list and student detail by `playerProfileId`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers API entry points, application queries, DTOs, authorization, data contract, tests, and validation docs for one read-only feature.
- **Existing architecture wins**: PASS. The design uses current controllers, MediatR, repositories, EF Core, BaseResponse, GenericException, and existing pagination types.
- **SaaS data isolation**: PASS. All list/detail reads are scoped by route `communityId`, active Owner/Teacher membership, and matching `StudentLicense.CommunityId`.
- **JSON where flexibility matters**: PASS. No question JSON behavior changes.
- **Minimum useful implementation**: PASS. The feature is limited to viewing students and placeholder analytics; no mutation, assignments, dashboards, reports, exports, imports, or analytics calculation.
- **Quality gates**: PASS. Implementation must run `dotnet build SprintLabs.sln`; focused tests and `dotnet test SprintLabs.sln` are planned for non-trivial authorization/filtering logic.
- **Documentation is executable context**: PASS. Plan, data model, OpenAPI contract, and quickstart live under `specs/009-community-student-viewing/`.

## Project Structure

### Documentation (this feature)

```text
specs/009-community-student-viewing/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- community-students-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md              # Generated later by /speckit-tasks
```

Implementation tasks should also create frontend-facing docs required by `AGENTS.md`:

```text
specs/009-community-student-viewing/
|-- api.md
`-- frontend.md
```

### Source Code (repository root)

```text
API/
`-- Controllers/
    `-- CommunitiesController.cs

Application/
`-- Features/
    `-- Communities/
        `-- Students/
            |-- Common/
            |   |-- CommunityStudentAuthorization.cs
            |   |-- CommunityStudentListItemResponse.cs
            |   |-- CommunityStudentDetailResponse.cs
            |   |-- CommunityStudentAnalyticsResponse.cs
            |   `-- CommunityStudentValidation.cs
            |-- ListStudents/
            |   |-- ListStudentsQuery.cs
            |   `-- ListStudentsQueryHandler.cs
            `-- GetStudentDetail/
                |-- GetStudentDetailQuery.cs
                `-- GetStudentDetailQueryHandler.cs

SprintLabs.Tests/
`-- Features/
    `-- CommunityStudentViewing/
        |-- ListStudentsQueryHandlerTests.cs
        `-- GetStudentDetailQueryHandlerTests.cs
```

**Structure Decision**: Use a new read-only `Communities/Students` feature area instead of modifying `StudentLicenses` mutation features. Keep common authorization, validation, and DTOs in `Application/Features/Communities/Students/Common/` so list and detail share Owner/Teacher access checks and grade/class validation without creating repositories or broad abstractions.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Use `StudentLicenses` as the source of community students.
- Use `ICommunityAccessService.HasCommunityRole` with `Owner` and `Teacher` for both list and detail.
- Use existing `PagedRequest` and `PagedResponse<T>` for list pagination.
- Query related grade/class/player/user information only to shape DTOs and search, not to expose entities directly.
- Validate optional grade and class filters before applying them.
- Return detail only for non-revoked student licenses linked to the requested player profile in the route community.
- Include placeholder analytics with default values only.
- Do not add a migration.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/community-students-api.openapi.yaml](./contracts/community-students-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design preserves one vertical read-only roster slice with controller, queries, DTOs, tests, contracts, and validation guide.
- **Existing architecture wins**: PASS. No new architecture, repositories, pagination model, auth model, package, database object, or migration is introduced.
- **SaaS data isolation**: PASS. Community ownership is enforced by active Owner/Teacher membership and `StudentLicense.CommunityId` matching the route.
- **JSON where flexibility matters**: PASS. Not applicable.
- **Minimum useful implementation**: PASS. Analytics remains placeholder; unrelated workflows are excluded.
- **Quality gates**: PASS. Build/test expectations are documented in quickstart and should become tasks.
- **Documentation is executable context**: PASS. Generated artifacts live beside the feature spec.

## Complexity Tracking

No constitution violations.
