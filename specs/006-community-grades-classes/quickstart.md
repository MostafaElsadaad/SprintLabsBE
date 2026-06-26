# Quickstart: Community Grades and Classes

## Purpose

Validate community grade and class management end to end after implementation.

## Prerequisites

- The User Identity, Admin Community, Community Access, Owner Community Profile, and Owner Teacher Management features are applied.
- A local MySQL database is configured.
- The Community Grades and Classes migration has been applied.
- Test users have valid Sprint Labs JWTs containing `userId`.
- A community exists with:
  - Active Owner membership
  - Active Teacher membership
  - Active Student membership
  - Pending or Removed membership
  - A platform admin without membership
- A second community exists for cross-community grade/class ownership tests.

## Build and Test

From the repository root:

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln
```

If local MySQL configuration prevents migration/manual API validation, record the limitation and still complete build plus handler tests.

## Scenario 1: Owner or Teacher Creates Grade

```http
POST /api/v1/Communities/{communityId}/grades
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
Content-Type: application/json

{
  "name": "Grade 5",
  "sortOrder": 5
}
```

Expected:

- HTTP 200 according to existing controller conventions
- Response includes `id`, `communityId`, `name`, `sortOrder`, `classCount`, and `createdAt`
- `classCount` is `0`

## Scenario 2: List Grades With Class Counts

```http
GET /api/v1/Communities/{communityId}/grades
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
```

Expected:

- HTTP 200
- Only grades from the route community are returned
- Class counts exclude deleted classes
- Grades are ordered by `sortOrder` then name

## Scenario 3: Create Class Under Grade

```http
POST /api/v1/Communities/{communityId}/classes
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
Content-Type: application/json

{
  "gradeId": 1,
  "name": "Class A"
}
```

Expected:

- HTTP 200
- Response includes `id`, `communityId`, `gradeId`, `name`, `status`, and `createdAt`
- `status` is `Active`

Repeat with a `gradeId` from another community.

Expected:

- Controlled 400 or 404 response
- No class is created

## Scenario 4: List Classes

```http
GET /api/v1/Communities/{communityId}/classes
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
```

Expected:

- HTTP 200
- Only Active classes from the route community are returned
- Deleted classes are excluded

With grade filter:

```http
GET /api/v1/Communities/{communityId}/classes?gradeId=1
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
```

Expected:

- Only Active classes under the requested grade are returned
- A grade from another community is rejected

## Scenario 5: Update Class

```http
PATCH /api/v1/Communities/{communityId}/classes/{classId}
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
Content-Type: application/json

{
  "name": "Updated Class A",
  "gradeId": 2
}
```

Expected:

- HTTP 200
- Name is trimmed and updated
- Class moves only if grade 2 belongs to the same community
- Moving to another community's grade is rejected
- Updating a deleted class is rejected

## Scenario 6: Soft Delete Class

```http
DELETE /api/v1/Communities/{communityId}/classes/{classId}
Authorization: Bearer OWNER_OR_TEACHER_TOKEN
```

Expected:

- HTTP 200 according to existing conventions
- Stored class status becomes `Deleted`
- Class is not hard deleted
- Class no longer appears in list classes
- Grade class count excludes the deleted class

## Scenario 7: Role Denial

Run create/list/update/delete actions as:

- Active Student
- Pending Owner or Teacher
- Removed Owner or Teacher
- User without membership
- Platform admin without Active Owner/Teacher membership
- Unauthenticated caller

Expected:

- 401 for unauthenticated
- 403 or existing forbidden equivalent for authenticated users without Active Owner/Teacher membership
- No grade or class mutation

## Scope Verification

Confirm the implementation adds:

- One migration for Grades and Classes
- No student license, student assignment, analytics, payment, import/export, dashboard, or grade update/delete endpoints
- No custom repository
- No new permissions framework

See [data-model.md](./data-model.md) and [the API contract](./contracts/grades-classes-api.openapi.yaml) for expected fields and authorization.
