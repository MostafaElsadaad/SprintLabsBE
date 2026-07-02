# Quickstart: Community Student Viewing

## Purpose

Validate the read-only community student viewing feature end to end after implementation.

Related artifacts:

- [spec.md](./spec.md)
- [plan.md](./plan.md)
- [data-model.md](./data-model.md)
- [contracts/community-students-api.openapi.yaml](./contracts/community-students-api.openapi.yaml)

## Prerequisites

- Existing SprintLabs solution restored.
- Database has migrations applied through the previous student license activation feature.
- Test data includes:
  - One community with an Active Owner.
  - One community with an Active Teacher.
  - One Active Student member.
  - One user with no membership.
  - One Pending membership and one Removed membership for negative checks.
  - Grades and classes in the community.
  - Student licenses across Pending, Active, and Revoked statuses.
  - At least one Active student license linked to a `User` and `Player` profile.
  - At least one Pending student license with null `UserId` and null `PlayerProfileId`.
  - At least one student license in another community.

## Build and Test

From repository root:

```bash
dotnet build SprintLabs.sln
```

For implementation validation:

```bash
dotnet test SprintLabs.sln
```

If database-backed integration tests require a local MySQL instance and cannot run in the environment, record that limitation and still run `dotnet build SprintLabs.sln`.

## Manual Validation Scenarios

### 1. Owner Lists Students

1. Authenticate as an Active Owner for the test community.
2. Call `GET /api/v1/Communities/{communityId}/students?pageNumber=1&pageSize=10`.
3. Confirm response status is 200.
4. Confirm the response data is paginated.
5. Confirm list items include `licenseId`, `email`, `status`, nullable `userId`, nullable `playerProfileId`, nullable `playerName`, nullable `avatarUrl`, `gradeId`, `gradeName`, `classId`, `className`, nullable `activatedAt`, and `createdAt`.
6. Confirm no students from another community appear.

### 2. Teacher Lists Students

1. Authenticate as an Active Teacher for the test community.
2. Call `GET /api/v1/Communities/{communityId}/students`.
3. Confirm response status is 200.
4. Confirm the same community-scoped roster is returned.

### 3. Unauthorized Roles Cannot List

1. Authenticate as a Student in the community and call the list endpoint.
2. Authenticate as a Pending or Removed member and call the list endpoint.
3. Authenticate as a user with no membership and call the list endpoint.
4. Authenticate as a platform-admin-only user without Owner/Teacher membership and call the list endpoint.
5. Confirm each authenticated request is denied.
6. Call the endpoint with no token and confirm 401.

### 4. Filters Work

1. Call the list endpoint with `gradeId` for a valid route-community grade.
2. Confirm only students assigned to that grade are returned.
3. Call with `classId` for a valid route-community class.
4. Confirm only students assigned to that class are returned.
5. Call with both `gradeId` and `classId`.
6. Confirm only students matching both filters are returned.
7. Call with `status=Pending`, `status=Active`, and `status=Revoked`.
8. Confirm each response only includes the requested status.

### 5. Invalid Filters Are Rejected

1. Call the list endpoint with a `gradeId` from another community.
2. Call the list endpoint with a `classId` from another community.
3. Call the list endpoint with a deleted class.
4. Call the list endpoint with a valid community grade and a class from a different grade.
5. Confirm each request is rejected with the existing project error style.

### 6. Search Works

1. Search by pending student license email.
2. Search by active linked user email.
3. Search by linked user or player name.
4. Confirm matching students are returned and other-community matches are excluded.

### 7. Pending Students Remain Nullable

1. Ensure a Pending student license has null `UserId` and null `PlayerProfileId`.
2. Call the list endpoint.
3. Confirm the pending student appears with nullable user/player fields and still includes grade/class data.

### 8. Student Detail Works

1. Authenticate as Active Owner or Teacher.
2. Call `GET /api/v1/Communities/{communityId}/students/{playerProfileId}` for a player linked to a non-revoked student license in the community.
3. Confirm response includes profile, progression, license, grade/class, and placeholder analytics fields.
4. Confirm analytics values are null, zero, or defaults only.

### 9. Student Detail Is Community-Scoped

1. Call detail for a player profile linked only to another community's student license.
2. Confirm not found.
3. Call detail for a player profile linked only to a Revoked license in the route community.
4. Confirm not found.

## Expected Non-Changes

- No new database tables.
- No migration.
- No student license create/update/revoke/activation behavior changes.
- No assignment, report, dashboard, export, import, parent account, or real analytics behavior.
