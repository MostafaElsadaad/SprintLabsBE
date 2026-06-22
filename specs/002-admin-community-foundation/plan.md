# Implementation Plan: Admin Community Foundation

**Branch**: `002-admin-community-foundation` | **Date**: 2026-06-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-admin-community-foundation/spec.md`

## Summary

Build the platform-admin foundation for Sprint Labs B2B school/community management. The implementation will add Community, CommunityUser, and CommunityLicense data models with MySQL-safe constraints; expose admin-only community creation, listing, owner assignment, and license upsert endpoints; and enforce platform-admin access from the existing User Identity Foundation using `Users.IsPlatformAdmin` and the current JWT `userId` claim.

## Technical Context

**Language/Version**: C# on .NET 8

**Primary Dependencies**: ASP.NET Core API, MediatR, EF Core, Pomelo MySQL provider, existing IdentityDbContext/UserManager infrastructure, BaseResponse, GenericException

**Storage**: MySQL through EF Core migrations

**Testing**: `dotnet build SprintLabs.sln`; focused tests in `SprintLabs.Tests` where repository/handler behavior can run with existing test infrastructure

**Target Platform**: SprintLabs backend API

**Project Type**: Monolith backend API with Domain, Application, Infrastructure, API, Shared, and test projects

**Performance Goals**: Platform admins can complete each community setup action in one request; community listing should remain efficient by loading owner and license summaries in one repository query path

**Constraints**: Follow existing CQRS/MediatR, controller, repository, EF Core, MySQL, BaseResponse, and GenericException conventions; do not introduce ASP.NET Identity or rewrite authentication; keep changes minimal and inside this feature scope; no teacher, student, grade, class, student-license, analytics, dashboard, invite email, or payment logic

**Scale/Scope**: Admin foundation only for Communities, CommunityUsers, CommunityLicenses, and `/api/admin/*` platform-admin access; future teacher/student flows are intentionally excluded

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Vertical slices over layer-first work**: PASS. The planned work is one admin community slice covering schema, domain/application logic, API contract, tests/verification, and docs.
- **Existing architecture wins**: PASS. The plan uses current ASP.NET Core controllers, MediatR handlers, Domain/Application/Infrastructure/Shared separation, repositories, EF Core, MySQL, BaseResponse, and GenericException.
- **SaaS data isolation**: PASS. Community ownership is explicit through CommunityId on memberships and licenses; admin-only access is scoped by platform admin identity.
- **JSON where flexibility matters**: PASS. No JSON payload storage is introduced.
- **Minimum useful implementation**: PASS. The plan limits work to communities, owner membership, license limits, and admin access checks.
- **Quality gates**: PASS. Implementation must run `dotnet build SprintLabs.sln`; focused tests should be added for non-trivial authorization, duplicate, and upsert behavior where existing test infrastructure supports it.
- **Documentation is executable context**: PASS. Spec, plan, research, data model, contracts, and quickstart live in `specs/002-admin-community-foundation/`.

## Project Structure

### Documentation (this feature)

```text
specs/002-admin-community-foundation/
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/
    admin-communities.openapi.yaml
  checklists/
    requirements.md
  tasks.md
```

### Source Code (repository root)

```text
API/
  Controllers/
    AdminCommunitiesController.cs

Application/
  Features/
    Admin/
      Communities/
        CreateCommunity/
        ListCommunities/
        AssignOwner/
        UpsertCommunityLicense/

Domain/
  Enums/
    CommunityStatus.cs
    CommunityUserRole.cs
    CommunityUserStatus.cs
  Models/
    Community.cs
    CommunityUser.cs
    CommunityLicense.cs
  Repositories/
    ICommunityRepository.cs
  Services/
    IUserService.cs

Infrastructure/
  DataAccess/
    ApplicationDbContext.cs
    User.cs
  Repositories/
    CommunityRepository.cs
  Services/
    UserService.cs
  Migrations/

Shared/
  Responses/
    Admin community response DTOs if shared by existing convention only

SprintLabs.Tests/
  Features/
    AdminCommunityFoundation tests where existing fixtures support them
```

**Structure Decision**: Use the existing monolith backend layout. Domain owns community entities/enums and repository interface. Infrastructure owns EF configuration, migrations, and repository implementation. Application owns CQRS commands/queries, handlers, and endpoint DTOs. API owns only routing, authorization attribute, `userId` claim extraction, BaseResponse wrapping, and MediatR dispatch.

## Complexity Tracking

No constitution violations identified.

## Phase 0: Research Summary

See [research.md](./research.md). All planning decisions are resolved; no `NEEDS CLARIFICATION` items remain.

## Phase 1: Design Summary

See [data-model.md](./data-model.md), [contracts/admin-communities.openapi.yaml](./contracts/admin-communities.openapi.yaml), and [quickstart.md](./quickstart.md).

## Post-Design Constitution Check

- **Vertical slices over layer-first work**: PASS. The design maps each admin action to a complete CQRS/API slice and includes schema, contracts, and verification.
- **Existing architecture wins**: PASS. No new packages, auth rewrite, or framework changes are planned.
- **SaaS data isolation**: PASS. Community data is keyed by CommunityId; membership uniqueness prevents duplicate user/community relationships; admin access is based on platform user identity.
- **JSON where flexibility matters**: PASS. No JSON persistence changes.
- **Minimum useful implementation**: PASS. Teachers/students/classes/licenses-for-students/analytics/payments remain excluded.
- **Quality gates**: PASS. Build is required after implementation; tests are planned for authorization, duplicate slug, owner idempotency, and license upsert behavior.
- **Documentation is executable context**: PASS. Plan points to all generated design artifacts.
