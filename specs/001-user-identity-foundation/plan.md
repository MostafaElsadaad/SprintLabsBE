# Implementation Plan: User Identity Foundation

**Branch**: `001-user-identity-foundation` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-user-identity-foundation/spec.md`

## Summary

Introduce a shared `User` login identity while preserving the current player login/profile behavior. The implementation will extend the existing `Infrastructure.DataAccess.User` identity entity already used by `ApplicationDbContext`, keep the current `Player` table as the player profile, add a nullable `Player.UserId` relationship for safe rollout, update Google login to find/create users by email and player profiles by game flow, and add current-user read endpoints through the existing controller/MediatR/BaseResponse conventions.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core API, API versioning, MediatR, EF Core, Pomelo MySQL provider, existing ASP.NET IdentityCore wiring, JWT bearer authentication

**Storage**: MySQL through EF Core migrations

**Testing**: `dotnet build SprintLabs.sln`; focused `dotnet test SprintLabs.sln` where test project setup allows identity/repository handler tests

**Target Platform**: Backend web API

**Project Type**: Monolith backend API with Domain/Application/Infrastructure/Shared/API projects

**Performance Goals**: Current-user reads and Google login should remain single-user operations with indexed identity/profile lookup by email, GoogleId, UserId, and authenticated user id.

**Constraints**: Minimal local changes only; do not rewrite auth; do not introduce new packages; do not implement communities, licenses, teachers, owners, admin community management, grades/classes, or student licenses; keep `Player.UserId` nullable in the first migration.

**Scale/Scope**: One vertical identity foundation slice covering Users, Player-to-User link, Google login response/claims, `GET /api/users/me`, and `GET /api/users/me/player-profile`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. Plan covers schema, domain/application changes, API contracts, and validation for one identity slice.
- **Existing architecture wins**: PASS. Uses existing API controllers, MediatR handlers, EF Core DbContext, IdentityCore user entity, repositories, BaseResponse, and GenericException patterns.
- **SaaS data isolation**: PASS. No B2B tenant/community data is introduced; identity is platform-wide and player-profile access is scoped to the authenticated user.
- **JSON where flexibility matters**: PASS. No question JSON changes.
- **Minimum useful implementation**: PASS. No new packages, generic abstractions, or future community/license models.
- **Quality gates**: PASS. Implementation phase must run `dotnet build SprintLabs.sln`; tests should be added/run for non-trivial handler and repository behavior where feasible.
- **Documentation is executable context**: PASS. Plan, research, data model, contracts, and quickstart live under `specs/001-user-identity-foundation/`.

## Project Structure

### Documentation (this feature)

```text
specs/001-user-identity-foundation/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- users-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

### Source Code (repository root)

```text
API/
|-- Controllers/
|   |-- AccountController.cs
|   `-- UsersController.cs

Application/
|-- Features/
|   |-- Accounts/
|   |   `-- GoogleAuthenticate/
|   |       |-- GoogleAuthenticationCommand.cs
|   |       `-- GoogleAuthenticationCommandHandler.cs
|   `-- Users/
|       |-- GetCurrentUser/
|       |   |-- GetCurrentUserQuery.cs
|       |   |-- GetCurrentUserQueryHandler.cs
|       |   `-- CurrentUserResponse.cs
|       `-- GetCurrentPlayerProfile/
|           |-- GetCurrentPlayerProfileQuery.cs
|           `-- GetCurrentPlayerProfileQueryHandler.cs

Domain/
|-- Enums/
|   `-- UserStatus.cs
|-- Models/
|   `-- Player.cs
|-- Repositories/
|   |-- IPlayerRepository.cs
|   `-- IUserRepository.cs

Infrastructure/
|-- DataAccess/
|   |-- ApplicationDbContext.cs
|   `-- User.cs
|-- Repositories/
|   |-- PlayerRepository.cs
|   `-- UserRepository.cs
`-- Migrations/

Shared/
`-- Responses/
    |-- LoginResponse.cs
    |-- PlayerProfileResponse.cs
    `-- BaseResponse.cs

SprintLabs.Tests/
`-- [focused handler/repository tests if feasible]
```

**Structure Decision**: Use the existing monolith solution structure. Keep controllers thin in `API`, CQRS features in `Application`, repository interfaces/enums/models in `Domain`, EF Core entity configuration/repositories/migrations in `Infrastructure`, and response DTOs in `Shared` only when reused at controller boundaries.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Extend existing `Infrastructure.DataAccess.User : IdentityUser<long>` rather than adding a second user table/entity.
- Keep `Player` as the player profile and add nullable `UserId` for first migration rollout safety.
- Use email as the primary Google login user match, then persist/update GoogleId when available.
- Include both `UserId` and existing player identifier claims in JWT for current-user resolution and backward compatibility.
- Use EF Core/MySQL indexes with a unique nullable `Player.UserId` index and verify generated migration behavior.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/users-api.openapi.yaml](./contracts/users-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slice**: PASS. Artifacts cover database, auth flow, API contracts, and validation.
- **Existing architecture**: PASS. Design follows current projects and naming patterns.
- **SaaS isolation**: PASS. Current-user reads are scoped by authenticated user id; no tenant/community data introduced.
- **Minimum useful implementation**: PASS. User repository is included only for user-specific persistence lookups not cleanly covered by `IPlayerRepository`; no generic abstractions or new packages.
- **Quality gates**: PASS. Quickstart requires `dotnet build SprintLabs.sln` after implementation.

## Complexity Tracking

No constitution violations.
