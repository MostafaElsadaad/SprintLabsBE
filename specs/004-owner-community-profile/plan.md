# Implementation Plan: Owner Community Profile

**Branch**: `004-owner-community-profile` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-owner-community-profile/spec.md`

## Summary

Add authenticated community profile read and update endpoints without changing the existing data model. A new thin `CommunitiesController` will dispatch a MediatR query for profile reads and a command for profile updates. The handlers will reuse `ICommunityAccessService.CanAccessCommunity` for reads and `HasCommunityRole` with `CommunityUserRole.Owner` for updates, then query or update `Community` through the existing generic repository. The update flow will normalize the slug consistently with admin community creation, exclude the current community from duplicate checks, update `UpdatedAt`, and return DTOs only.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>`, `ICommunityAccessService`, `IUserService`, `BaseResponse`, and `GenericException`

**Storage**: Existing MySQL `Communities`, `CommunityUsers`, and `Users` tables; no schema or migration changes

**Testing**: Focused handler tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` where the existing test environment permits

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Each profile read or update performs bounded identity, membership, and community lookups; no collection scan or unrelated aggregate loading

**Constraints**: Follow existing CQRS/controller/DTO/error patterns; use existing community access service; use generic repositories; do not rewrite authentication; do not add a permissions framework, tables, migrations, packages, or future community-management scope

**Scale/Scope**: Two versioned endpoints, one shared response DTO, one query slice, one command slice, one controller, focused tests, and feature documentation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers both API slices, authorization, persistence behavior, tests, contracts, and frontend-facing docs.
- **Existing architecture wins**: PASS. The design uses the current controller, MediatR, generic repository, access service, BaseResponse, and GenericException patterns.
- **SaaS data isolation**: PASS. Every operation is scoped by authenticated user id, exact community id, Active membership, and Owner role where required.
- **JSON where flexibility matters**: PASS. No question JSON or JSON persistence behavior changes.
- **Minimum useful implementation**: PASS. No new permission framework, service abstraction, repository, schema, or future owner-management workflow is introduced.
- **Quality gates**: PASS. Implementation requires focused authorization/slug tests, solution build, and tests where feasible.
- **Documentation is executable context**: PASS. Planning artifacts, API contract, quickstart, and frontend-facing docs remain under this feature folder.

## Project Structure

### Documentation (this feature)

```text
specs/004-owner-community-profile/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- api.md
|-- frontend.md
|-- contracts/
|   `-- communities-api.openapi.yaml
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
        |-- Common/
        |   `-- CommunityProfileResponse.cs
        |-- GetCommunityProfile/
        |   |-- GetCommunityProfileQuery.cs
        |   `-- GetCommunityProfileQueryHandler.cs
        `-- UpdateCommunityProfile/
            |-- UpdateCommunityProfileRequest.cs
            |-- UpdateCommunityProfileCommand.cs
            `-- UpdateCommunityProfileCommandHandler.cs

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
|-- Repositories/
|   `-- BaseRepository.cs
`-- Services/
    `-- CommunityAccessService.cs

SprintLabs.Tests/
`-- Features/
    `-- OwnerCommunityProfile/
        |-- GetCommunityProfileQueryHandlerTests.cs
        `-- UpdateCommunityProfileCommandHandlerTests.cs
```

**Structure Decision**: Add a single owner/community profile vertical feature under `Application/Features/Communities`, separate from platform-admin community management. Keep authorization inside handlers through the existing access service and keep the controller limited to routing, claim extraction, MediatR dispatch, and `BaseResponse` wrapping.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Add one versioned `CommunitiesController` at `api/v{version:apiVersion}/[controller]`, producing the required `/api/v1/Communities/{communityId}` route.
- Verify the authenticated user exists and is not suspended using the existing user service before community-specific authorization.
- Use `CanAccessCommunity` for GET and `HasCommunityRole(..., Owner)` for PATCH exactly as required.
- Return 403 for failed membership/role checks and avoid platform-admin bypass.
- Use `IBaseRepository<Community>.AsQueryable()` for long-id lookup and duplicate-slug checks; do not use the `int`-based `GetByIdAsync`.
- Normalize PATCH slugs with trim plus lowercase and reject duplicates only when owned by a different community.
- Use the existing `Community.UpdatedAt`; no migration is required.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/communities-api.openapi.yaml](./contracts/communities-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. Each endpoint has a complete controller-to-handler-to-persistence path and independent validation scenarios.
- **Existing architecture wins**: PASS. The contract and data design require no package, auth, repository, or framework changes.
- **SaaS data isolation**: PASS. The contract never accepts a user id and all access derives from the JWT user plus exact community membership.
- **JSON where flexibility matters**: PASS. Not applicable to this feature.
- **Minimum useful implementation**: PASS. Only basic profile read/update behavior is designed.
- **Quality gates**: PASS. Quickstart includes build, test, and manual role/access validation.
- **Documentation is executable context**: PASS. API and frontend behavior are explicitly documented beside the specification.

## Complexity Tracking

No constitution violations.
