# Implementation Plan: B2C Player Profile Support

**Branch**: `010-b2c-player-profile-support` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-b2c-player-profile-support/spec.md`

## Summary

Verify and complete B2C player profile support so direct-to-consumer players can sign in with Google, receive a User and PlayerProfile, view their own player profile, and update nullable profile fields without requiring `CommunityUser` or `StudentLicense` records. Existing Google login and `GET /api/v1/Users/me/player-profile` already follow the needed community-free shape; implementation should add or update a self-scoped `PATCH /api/v1/PlayerProfiles/me` slice if absent, preserve existing B2B student/teacher/owner login flows, and document future analytics context as nullable `CommunityId` without implementing analytics.

## Technical Context

**Language/Version**: C# / .NET 8

**Primary Dependencies**: ASP.NET Core API controllers and API versioning, MediatR, EF Core, Pomelo MySQL provider, existing `IPlayerRepository`, `IUserService`, `BaseResponse`, `GenericException`, existing Google authentication and player profile query/update patterns

**Storage**: Existing MySQL tables through EF Core: `Users` and `Players`. Current `Player` model already includes nullable `Age`, nullable `Grade`, and nullable `SchoolName`, so no migration is planned.

**Testing**: Focused handler tests in `SprintLabs.Tests`; login compatibility tests around existing Google authentication; `dotnet build SprintLabs.sln`; `dotnet test SprintLabs.sln` when implementing profile update and compatibility checks

**Target Platform**: SprintLabs backend web API

**Project Type**: .NET backend API monolith with API, Application, Domain, Infrastructure, Shared, and test projects

**Performance Goals**: Current-user profile reads and updates remain single-user operations scoped by authenticated user id.

**Constraints**: Follow existing SprintLabs CQRS/MediatR/controller/DTO/BaseResponse/GenericException/repository patterns; do not rewrite authentication; do not change community authorization rules; do not require `CommunityUser` or `StudentLicense` for B2C login/profile access; do not expose entities directly; do not add tables or migrations unless player profile fields are missing; do not implement real analytics.

**Scale/Scope**: Verification and one self-profile update endpoint, plus tests and frontend-facing documentation. No community membership, license, dashboard, report, assignment, parent account, payment, or analytics-calculation workflows.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The plan covers login/profile verification, self-profile update behavior, API contract, tests, and docs.
- **Existing architecture wins**: PASS. The design reuses current login, player repository, current-user profile query, MediatR, BaseResponse, and GenericException conventions.
- **SaaS data isolation**: PASS. B2C has no community boundary; B2B analytics context is documented as community-scoped for future work. No B2C path is allowed to infer or create community membership.
- **JSON where flexibility matters**: PASS. No question JSON behavior changes.
- **Minimum useful implementation**: PASS. The feature is limited to B2C login/profile support and analytics-context readiness.
- **Quality gates**: PASS. Implementation must run `dotnet build SprintLabs.sln`; focused tests and `dotnet test SprintLabs.sln` are planned.
- **Documentation is executable context**: PASS. Planning, contract, data-model, and quickstart artifacts live under `specs/010-b2c-player-profile-support/`.

## Project Structure

### Documentation (this feature)

```text
specs/010-b2c-player-profile-support/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- b2c-player-profile-api.openapi.yaml
|-- checklists/
|   `-- requirements.md
`-- tasks.md              # Generated later by /speckit-tasks
```

Implementation tasks should also create frontend-facing docs required by `AGENTS.md`:

```text
specs/010-b2c-player-profile-support/
|-- api.md
`-- frontend.md
```

### Source Code (repository root)

```text
API/
|-- Controllers/
|   |-- UsersController.cs                 # Existing GET /me/player-profile
|   `-- PlayerProfilesController.cs        # Add if PATCH /me is missing

Application/
`-- Features/
    |-- Users/
    |   `-- GetCurrentPlayerProfile/
    |       |-- GetCurrentPlayerProfileQuery.cs
    |       `-- GetCurrentPlayerProfileQueryHandler.cs
    `-- PlayerProfiles/
        `-- UpdateCurrentPlayerProfile/
            |-- UpdateCurrentPlayerProfileCommand.cs
            |-- UpdateCurrentPlayerProfileCommandHandler.cs
            `-- UpdateCurrentPlayerProfileRequest.cs

SprintLabs.Tests/
`-- Features/
    `-- B2CPlayerProfileSupport/
        |-- B2CLoginCompatibilityTests.cs
        |-- GetCurrentPlayerProfileQueryHandlerTests.cs
        `-- UpdateCurrentPlayerProfileCommandHandlerTests.cs
```

**Structure Decision**: Reuse the existing `Users/GetCurrentPlayerProfile` query for reads. Add a new `PlayerProfiles/UpdateCurrentPlayerProfile` CQRS slice only for the self-scoped PATCH endpoint, rather than extending the older `Account/profile` GoogleId-based update route. Keep current B2B activation/login code untouched except for tests proving compatibility.

## Phase 0: Research Summary

See [research.md](./research.md).

Resolved decisions:

- Treat Google login as already B2C-capable unless tests reveal a CommunityUser/StudentLicense dependency.
- Reuse `GET /api/v1/Users/me/player-profile`.
- Add `PATCH /api/v1/PlayerProfiles/me` if missing.
- Use authenticated `userId` claim to locate the current player profile for update.
- Do not use route/body profile ids for updates.
- No migration is needed because `Player.Age`, `Player.Grade`, and `Player.SchoolName` already exist.
- Document nullable `CommunityId` as analytics context only; do not add analytics storage or calculations.

## Phase 1: Design Summary

Design artifacts:

- [data-model.md](./data-model.md)
- [contracts/b2c-player-profile-api.openapi.yaml](./contracts/b2c-player-profile-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The plan includes current behavior verification, missing update endpoint, contract, docs, and tests.
- **Existing architecture wins**: PASS. No new architecture, package, permissions framework, auth rewrite, table, or migration is planned.
- **SaaS data isolation**: PASS. B2C profile flows are user-scoped; B2B analytics remains community-scoped in future documentation.
- **JSON where flexibility matters**: PASS. Not applicable.
- **Minimum useful implementation**: PASS. No analytics, reports, assignments, parent accounts, payment, membership, or license creation is included.
- **Quality gates**: PASS. Build/test validation is documented in quickstart.
- **Documentation is executable context**: PASS. Generated artifacts live beside the feature spec.

## Complexity Tracking

No constitution violations.
