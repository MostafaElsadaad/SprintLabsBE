# Quickstart: Owner Teacher Management

## Purpose

Validate owner teacher invitation, listing, removal, license accounting, and pending-teacher activation after implementation.

## Prerequisites

- The User Identity, Admin Community, Community Access, and Owner Community Profile features are applied.
- A local MySQL database is configured and current migrations are applied.
- Test users have valid Sprint Labs JWTs containing `userId`.
- A community exists with:
  - Active Owner membership
  - Active Teacher membership
  - Pending Teacher membership
  - Removed Teacher membership
  - Active Student or non-owner membership
  - CommunityLicense with `MaxTeachers` and `UsedTeachers`

## Build and Test

From the repository root:

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln
```

If local NuGet or MySQL configuration prevents tests, record the limitation and still complete the build plus manual API checks.

## Scenario 1: Owner Invites Teacher With Capacity

```http
POST /api/v1/Communities/{communityId}/teachers/invite
Authorization: Bearer OWNER_TOKEN
Content-Type: application/json

{
  "email": "teacher@example.com",
  "name": "Teacher Name"
}
```

Expected:

- HTTP 200 or 201 according to existing controller conventions
- Response includes `userId`, `name`, `email`, `status`, and `createdAt`
- `status` is `Pending`
- `UsedTeachers` increases by 1 only if this created or restored a counted teacher seat
- No duplicate `CommunityUser` row exists for the same user/community

## Scenario 2: Duplicate Invite Does Not Duplicate Seat

Repeat the same invite for a teacher who already has Pending or Active Teacher membership.

Expected:

- Controlled success or no-op response according to existing conventions
- Only one `CommunityUser` row exists for the user/community
- `UsedTeachers` does not increase again

## Scenario 3: Invite Rejected At Capacity

Set `UsedTeachers` equal to `MaxTeachers`, then invite a new teacher or restore a Removed Teacher.

Expected:

- Controlled 400 or equivalent validation error
- No membership is created or restored
- `UsedTeachers` remains unchanged

## Scenario 4: Non-Owners Cannot Manage Teachers

Call invite, list, and remove with Active Teacher, Active Student, Pending Owner, Removed Owner, no-membership, and platform-admin-without-owner tokens.

Expected:

- HTTP 403 or existing forbidden equivalent
- No membership or license changes occur

Call without a bearer token.

Expected:

- HTTP 401

## Scenario 5: Owner Lists Teachers

```http
GET /api/v1/Communities/{communityId}/teachers
Authorization: Bearer OWNER_TOKEN
```

Expected:

- HTTP 200
- Response list contains Teacher memberships for the requested community only
- Each row includes `userId`, `name`, `email`, `status`, and `createdAt`
- Teachers from other communities are not included

## Scenario 6: Owner Removes Teacher

```http
DELETE /api/v1/Communities/{communityId}/teachers/{userId}
Authorization: Bearer OWNER_TOKEN
```

Expected for Pending or Active Teacher:

- HTTP 200 according to existing conventions
- Membership status becomes `Removed`
- `UsedTeachers` decreases by 1
- Community access checks for that teacher return false

Expected for already Removed Teacher:

- No hard delete
- `UsedTeachers` does not decrease again

Expected for Owner target:

- Controlled error
- Owner membership is unchanged

## Scenario 7: Pending Teacher Activates On Matching Google Login

Create a Pending Teacher membership for `teacher@example.com`, then complete Google login with that email.

Expected:

- Existing Google login response still includes the existing user/player identity fields and JWT
- Matching Pending Teacher membership becomes `Active`
- `UsedTeachers` does not increase
- Pending Teacher memberships for other emails remain Pending

## Scope Verification

Confirm the implementation adds:

- No migration unless the existing schema is missing required fields
- No new database tables
- No custom repository
- No permissions framework
- No email sending
- No student, grade, class, student license, analytics, payment, admin teacher-management, or teacher dashboard endpoints

See [data-model.md](./data-model.md) and [the API contract](./contracts/teacher-management-api.openapi.yaml) for expected fields and authorization.
