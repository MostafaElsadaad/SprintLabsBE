# Implementation Plan: Student License Activation on Login

**Branch**: `008-student-license-activation` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-student-license-activation/spec.md`

## Summary

Activate pending student licenses inside the existing Google login flow. The implementation will reuse the current `GoogleAuthenticationCommandHandler`, shared `User` lookup, existing player-profile create/find behavior, `StudentLicense` fields, `CommunityUser` membership model, and generic repositories. No new endpoint, authentication rewrite, invitation system, database table, or migration is planned because the current student license schema already includes `UserId`, `PlayerProfileId`, `Status`, and `ActivatedAt`.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IBaseRepository<T>`, existing `IPlayerRepository`, `IUserService`, `IGoogleAuthenticationService`, `BaseResponse`, `GenericException`, and the current Google login handler

**Storage**: Existing MySQL tables via EF Core: `Users`, `Players`, `StudentLicenses`, `CommunityUsers`, and `CommunityLicenses`; no schema change planned

**Testing**: Focused handler tests in `SprintLabs.Tests`; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` for activation, idempotency, ignored statuses, membership restore/create behavior, and login compatibility

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Google login remains a single-user operation; pending student activation queries are scoped by normalized email and Pending status, and membership lookups are scoped by activated license community and resolved user id

**Constraints**: Extend the existing Google login flow only; do not rewrite authentication; do not introduce ASP.NET Identity changes, a new invitation system, a new endpoint, new packages, new tables, or a migration unless required fields are missing; preserve existing pending-teacher activation and B2C player login behavior; do not increment `CommunityLicense.UsedStudents` during activation

**Scale/Scope**: One login activation touchpoint, no public contract shape change beyond documented side effects, focused tests, and frontend-facing documentation during implementation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers the login activation behavior, affected persistence records, contract notes, tests, and validation guide.
- **Existing architecture wins**: PASS. The design reuses the current Google login handler, repositories, entities, enums, and test style.
- **SaaS data isolation**: PASS. Activation is scoped by matched student license community, resolved user id, and same-community membership uniqueness.
- **JSON where flexibility matters**: PASS. No question JSON or flexible payload behavior changes.
- **Minimum useful implementation**: PASS. No dashboard, invitation acceptance flow, email sending, analytics, payment, grade/class, or owner-management changes are included.
- **Quality gates**: PASS. Implementation requires focused tests plus `dotnet build SprintLabs.sln` and `dotnet test SprintLabs.sln` where feasible.
- **Documentation is executable context**: PASS. Planning artifacts, contract notes, and quickstart live beside the feature spec.

## Project Structure

### Documentation (this feature)

```text
specs/008-student-license-activation/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- google-login-student-activation.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md
```

`tasks.md` is generated later by `/speckit-tasks`.

### Source Code (repository root)

```text
Application/
`-- Features/
    `-- Accounts/
        `-- GoogleAuthenticate/
            |-- GoogleAuthenticationCommand.cs
            |-- GoogleAuthenticationCommandHandler.cs
            `-- GoogleAuthResponse.cs

Domain/
|-- Enums/
|   `-- CommunityEnums.cs
|-- Models/
|   |-- Community.cs
|   |-- Player.cs
|   `-- StudentLicense.cs
|-- Repositories/
|   |-- IBaseRepository.cs
|   `-- IPlayerRepository.cs
`-- Services/
    |-- IGoogleAuthenticationService.cs
    `-- IUserService.cs

Infrastructure/
|-- DataAccess/
|   |-- ApplicationDbContext.cs
|   `-- User.cs
`-- Repositories/
    |-- BaseRepository.cs
    `-- PlayerRepository.cs

SprintLabs.Tests/
`-- Features/
    `-- StudentLicenseActivation/
        `-- StudentLicenseActivationOnLoginTests.cs
```

**Structure Decision**: Keep activation inside the existing account login feature because there is no new API endpoint or owner/student-facing command. Add only the repository dependencies needed by `GoogleAuthenticationCommandHandler` and keep activation as private handler logic unless implementation size proves a tiny helper method clearer. Use `IBaseRepository<StudentLicense>` and `IBaseRepository<CommunityUser>` for simple EF Core queries and updates, and keep `IPlayerRepository` for existing player profile lookup/create behavior.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Activate student licenses after shared `User` resolution and after the `Player` profile is found or created.
- Query `StudentLicenses` by normalized email and `Status = Pending`.
- Activate all valid pending licenses across communities in one login.
- Link activated licenses to the resolved `User` and `Player`.
- Create or restore `CommunityUser` with `Role = Student` and `Status = Active`.
- Do not increment `CommunityLicense.UsedStudents` during activation.
- Preserve existing pending-teacher activation order and login response fields.
- Do not add a migration because required student license fields already exist.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/google-login-student-activation.openapi.yaml](./contracts/google-login-student-activation.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design includes login behavior, data transitions, contract notes, and validation scenarios.
- **Existing architecture wins**: PASS. No new endpoint, package, repository abstraction, table, or authentication framework is required.
- **SaaS data isolation**: PASS. Community access is created only for the community attached to the activated student license and the resolved login user.
- **JSON where flexibility matters**: PASS. Not applicable to this feature.
- **Minimum useful implementation**: PASS. The plan is limited to activation on Google login and does not expand owner/student management.
- **Quality gates**: PASS. Quickstart includes build, tests, and manual verification for activation and idempotency.
- **Documentation is executable context**: PASS. The contract and validation steps are captured under `specs/008-student-license-activation`.

## Complexity Tracking

No constitution violations.
