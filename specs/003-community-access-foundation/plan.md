# Implementation Plan: Community Access Foundation

**Branch**: `003-community-access-foundation` | **Date**: 2026-06-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-community-access-foundation/spec.md`

## Summary

Build reusable community membership and role access checks on top of the existing Community and CommunityUser model, then expose `GET /api/users/me/communities` so authenticated users can retrieve only their own active community memberships. The implementation will preserve the completed platform-admin authorization model and keep community role access separate from `Users.IsPlatformAdmin`.

## Technical Context

**Language/Version**: C# on .NET 8

**Primary Dependencies**: ASP.NET Core API, MediatR, EF Core, Pomelo MySQL provider, existing repository pattern, BaseResponse, GenericException

**Storage**: Existing MySQL tables from Admin Community Foundation; no new schema is planned

**Testing**: `dotnet build SprintLabs.sln`; focused handler/service tests in `SprintLabs.Tests` where existing test infrastructure supports them

**Target Platform**: SprintLabs backend API

**Project Type**: Monolith backend API with Domain, Application, Infrastructure, API, Shared, and test projects

**Performance Goals**: Current-user community list loads all active memberships for the authenticated user in one repository query path; access and role checks use direct membership lookups

**Constraints**: Follow existing CQRS/MediatR, controller, repository, EF Core, BaseResponse, and GenericException conventions; do not introduce ASP.NET Identity or rewrite authentication; do not modify existing platform-admin authorization unless needed to avoid conflicts; do not add teacher/student management, grades, classes, student licenses, analytics, payments, or owner dashboard behavior

**Scale/Scope**: Community access decisions, community role decisions for Owner/Teacher/Student, reusable helper/service, and `GET /api/users/me/communities` only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The feature includes reusable domain/application access behavior, repository support, API endpoint, tests/verification, and docs.
- **Existing architecture wins**: PASS. The plan uses current controllers, MediatR handlers, Domain/Application/Infrastructure separation, repositories, BaseResponse, and GenericException.
- **SaaS data isolation**: PASS. Community access is explicitly scoped by `CommunityUser.UserId`, `CommunityId`, role, and Active status.
- **JSON where flexibility matters**: PASS. No JSON persistence changes are introduced.
- **Minimum useful implementation**: PASS. The plan avoids new management endpoints and implements only membership/role checks plus the current-user community list.
- **Quality gates**: PASS. Implementation must run `dotnet build SprintLabs.sln`; tests are planned for access and role-check logic where feasible.
- **Documentation is executable context**: PASS. Spec, plan, research, data model, contract, quickstart, and later tasks live under `specs/003-community-access-foundation/`.

## Project Structure

### Documentation (this feature)

```text
specs/003-community-access-foundation/
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/
    users-communities.openapi.yaml
  checklists/
    requirements.md
  tasks.md
```

### Source Code (repository root)

```text
API/
  Controllers/
    UsersController.cs

Application/
  Features/
    Users/
      GetCurrentUserCommunities/
        GetCurrentUserCommunitiesQuery.cs
        GetCurrentUserCommunitiesQueryHandler.cs
        UserCommunityResponse.cs

Domain/
  Repositories/
    ICommunityRepository.cs
  Services/
    ICommunityAccessService.cs

Infrastructure/
  Repositories/
    CommunityRepository.cs
  Services/
    CommunityAccessService.cs
  ServiceConfig.cs

SprintLabs.Tests/
  Features/
    CommunityAccessFoundation/
```

**Structure Decision**: Extend the current Admin Community Foundation pieces rather than adding new tables. Repository methods should read active memberships and community summaries. A small `ICommunityAccessService`/`CommunityAccessService` is acceptable because the feature explicitly requires reusable access and role checks for future endpoints. The `UsersController` remains thin and dispatches a MediatR query for `GET /api/users/me/communities`.

## Complexity Tracking

No constitution violations identified.

## Phase 0: Research Summary

See [research.md](./research.md). All planning decisions are resolved; no `NEEDS CLARIFICATION` items remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/users-communities.openapi.yaml](./contracts/users-communities.openapi.yaml), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design maps reusable access checks and the current-user communities endpoint to existing project layers.
- **Existing architecture wins**: PASS. No auth rewrite, ASP.NET Identity addition, new package, or new controller pattern is planned.
- **SaaS data isolation**: PASS. Access decisions are based on active membership for the requested user and community only.
- **JSON where flexibility matters**: PASS. No JSON behavior is introduced.
- **Minimum useful implementation**: PASS. Filters are deferred unless implementation finds an existing clean pattern; service methods provide the reusable core.
- **Quality gates**: PASS. Build is required after implementation; focused tests are planned for active/pending/removed/no-membership and role-match cases.
- **Documentation is executable context**: PASS. Generated artifacts are stored under the feature directory.
