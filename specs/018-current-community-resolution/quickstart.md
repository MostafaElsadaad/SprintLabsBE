# Quickstart: Trusted Current-Community Resolution

## Purpose

Use this guide to verify the implementation of feature 018 after the implementation tasks are complete. Run all database-writing scenarios only against a dedicated development or test database.

## Prerequisites

- .NET 8 SDK and the repository's normal build dependencies
- A dedicated MySQL database for persistence and concurrency checks
- Test identities for PlatformAdmin, Owner, Teacher, Student, suspended user, and a user with no current staff membership
- Two Active communities and, for negative tests, one Suspended community

Do not use production identities or production membership data.

## Baseline Verification

From the repository root:

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln --no-build
```

After any model-related implementation change, confirm that the feature did not introduce a schema delta:

```powershell
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project API
```

Expected result: the solution builds, the tests pass, and feature 018 requires no migration. If the EF command reports a pre-existing model difference, investigate it separately rather than generating a feature-018 migration automatically.

## Focused Automated Checks

Run the focused tests added or updated for this feature. Adjust the filter only if the implementation task uses a more specific namespace.

```powershell
dotnet test SprintLabs.Tests/SprintLabs.Tests.csproj --no-build --filter "FullyQualifiedName~CommunityAccessServiceTests|FullyQualifiedName~CommunitiesControllerRouteContractTests|FullyQualifiedName~TeacherInvitationServiceTests|FullyQualifiedName~FirebaseLoginActivationTests|FullyQualifiedName~AssignOwnerCommandHandlerTests|FullyQualifiedName~CommunityLoginCommandHandlerTests|FullyQualifiedName~RefreshTokenServiceTests|FullyQualifiedName~AccessTokenServiceTests"
```

The focused suite must demonstrate:

- exactly one Active Owner or Teacher membership in an Active community resolves successfully;
- zero valid communities and multiple current staff communities fail closed;
- Pending, Removed, and Student memberships do not resolve tenant context;
- an Active staff membership plus a Pending or Active staff membership in another community is a conflict, regardless of whether the roles are Owner or Teacher;
- Removed staff memberships do not permanently block later assignment;
- staff access tokens do not contain CommunityId;
- the HTTP client cannot choose a tenant through a route, query, or request-body CommunityId.

## Manual API Scenarios

### 1. Owner current-community profile

Authenticate as an Active Owner of one Active community.

1. Call `GET /api/v1/Communities/me`.
2. Call `PATCH /api/v1/Communities/me` with the existing profile update body.
3. Confirm both operations target the Owner's database membership community.
4. Confirm no CommunityId is accepted or required in the path, query, or body.

Expected result: both requests succeed and the existing Owner-only update authorization remains enforced.

### 2. Teacher current-community profile

Authenticate as an Active Teacher of one Active community.

1. Call `GET /api/v1/Communities/me`.
2. Call `PATCH /api/v1/Communities/me`.

Expected result: GET succeeds; PATCH remains forbidden because tenant resolution does not grant Owner authorization.

### 3. Staff resource routes

Using appropriate Owner or Teacher identities, exercise the routes listed in `api.md` for teachers, grades, classes, student licenses, and students.

Expected result: each route derives CommunityId from the authenticated user, while existing handler-level role and tenant checks still accept or reject the operation according to the documented role matrix.

### 4. Zero and conflicting current staff memberships

Test each identity state:

- no Owner or Teacher membership;
- only a Pending Teacher membership;
- only a Removed Owner or Teacher membership;
- Active Owner in Community A plus Pending Teacher in Community B;
- Pending Owner in Community A plus Active Teacher in Community B;
- Active Teacher in Community A plus Active Teacher or Owner in Community B.

Expected result: current-community staff routes fail with the controlled access response. Login and refresh issue no usable staff tokens for conflicting legacy data.

### 5. Suspended states

Test an otherwise valid staff member when the user or community is suspended according to existing authentication fixtures.

Expected result: existing suspension behavior remains fail-closed. Current-community resolution never restores access that login or authorization already denies.

### 6. Cross-tenant identifiers

Authenticate as staff in Community A and send a `classId`, `licenseId`, `teacherUserId`, or `playerProfileId` belonging to Community B to a new tenantless route.

Expected result: the existing handler-level tenant check rejects the request. Server-side community resolution does not replace resource ownership checks.

### 7. Student compatibility

Authenticate as an Active Student and call:

```text
GET /api/v1/Communities/{the-student-community-id}
```

Expected result: the legacy community-profile endpoint continues to work for its existing Active Owner, Teacher, and Student membership rules. A Student does not gain access to `GET /api/v1/Communities/me` or other staff-only routes.

### 8. Platform Admin compatibility

Authenticate as a PlatformAdmin and exercise:

```text
POST  /api/v1/admin/communities/{communityId}/owner
PATCH /api/v1/admin/communities/{communityId}/licenses
```

Expected result: these routes retain their explicit CommunityId. PlatformAdmin status alone does not authorize membership-based `CommunitiesController` operations.

## Membership-Integrity Scenarios

Verify each write path below with cross-role data, not only same-role data.

| Path | Conflict setup | Expected result |
|---|---|---|
| Teacher invitation issue | invitee has Pending/Active Owner or Teacher membership in another community | controlled conflict; no new or restored invitation membership |
| Teacher invitation completion | invitee acquired Pending/Active Owner or Teacher membership in another community after issuance | controlled conflict; invitation and membership remain unchanged |
| Pending Teacher activation | user has Pending/Active Owner or Teacher membership in another community | controlled conflict; no token and no partial activation |
| PlatformAdmin owner assignment | target has Pending/Active Owner or Teacher membership in another community | controlled conflict; no membership overwrite |
| Community login | legacy conflicting current staff memberships | controlled authentication failure; no access/refresh tokens |
| Refresh | legacy conflicting current staff memberships | refresh fails; token is not rotated |

Also verify that a Removed staff membership in another community does not block a later invitation or owner assignment, subject to the existing target-community reuse rules documented in the plan.

## MySQL Concurrency Check

Run the opt-in integration test against a dedicated MySQL database. The exact environment variable and filter should follow the existing `MysqlDatabaseFixture` convention in `SprintLabs.Tests`.

The test should start two independent transactions that attempt to create or activate current staff memberships for the same user in different communities. Both paths must lock the same User row before checking memberships.

Expected result: one transaction succeeds and the other receives the controlled conflict after the first commits. The final database contains current Owner/Teacher memberships for at most one distinct community.

## Final Contract Inspection

Before handoff, confirm:

- `CommunitiesController` exposes every new tenantless route and preserves `GET /api/v1/Communities/{communityId:long}`;
- `AdminCommunitiesController` still exposes explicit CommunityId routes;
- CommunityId remains available only as server-populated internal command/query state for affected staff operations;
- no CommunityId claim was added to access or refresh tokens;
- no generic tenant middleware, global MediatR behavior, new package, or unrelated architecture was introduced;
- `api.md` and `frontend.md` match the implemented route templates and request DTOs exactly.
