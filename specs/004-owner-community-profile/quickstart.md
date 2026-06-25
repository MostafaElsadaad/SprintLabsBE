# Quickstart: Owner Community Profile

## Purpose

Validate community profile viewing and Owner-only editing end to end after implementation.

## Prerequisites

- The existing User Identity, Admin Community, and Community Access features are applied.
- A local MySQL database is configured and current migrations are applied.
- Test users have valid Sprint Labs JWTs containing `userId`.
- At least two communities exist.
- Membership fixtures include:
  - Active Owner
  - Active Teacher
  - Active Student
  - Pending Owner
  - Removed Owner
  - User with no membership

## Build and Test

From the repository root:

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln
```

If local NuGet or MySQL configuration prevents tests, record the limitation and still complete the build plus manual API checks.

## Validation Data

Use:

- Community A with slug `example-school`
- Community B with slug `other-school`
- One user for each membership case listed above
- A platform admin with no Community A membership

Do not alter production data to run these checks.

## Scenario 1: Active Members Can View

For the Active Owner, Teacher, and Student tokens:

```http
GET /api/v1/Communities/{communityAId}
Authorization: Bearer TOKEN
```

Expected:

- HTTP 200
- `data.id` equals Community A
- Response includes `name`, `slug`, and `status`
- No entity navigation or membership data is exposed

## Scenario 2: Inactive or Missing Membership Cannot View

Repeat GET using Pending, Removed, no-membership, and platform-admin-without-membership tokens.

Expected:

- HTTP 403
- No community profile data is returned

Call without a bearer token.

Expected:

- HTTP 401

## Scenario 3: Active Owner Updates Profile

```http
PATCH /api/v1/Communities/{communityAId}
Authorization: Bearer OWNER_TOKEN
Content-Type: application/json

{
  "name": "Updated School Name",
  "slug": "Updated-School-Slug"
}
```

Expected:

- HTTP 200
- Name is trimmed and returned as `Updated School Name`
- Slug is returned as `updated-school-slug`
- Status is unchanged
- A following GET returns the same updated values
- `UpdatedAt` is populated or advanced in storage

## Scenario 4: Same Slug Is Allowed

PATCH Community A using its current slug with different casing or surrounding whitespace.

Expected:

- HTTP 200
- Stored slug remains normalized

## Scenario 5: Another Community's Slug Is Rejected

PATCH Community A with `other-school`.

Expected:

- HTTP 400 controlled error
- Community A keeps its previous name and slug
- Community B is unchanged

## Scenario 6: Non-Owners Cannot Update

Repeat PATCH with Active Teacher, Active Student, Pending Owner, Removed Owner, no-membership, and platform-admin-without-membership tokens.

Expected:

- HTTP 403
- Community profile remains unchanged

## Scenario 7: Invalid Input

Test:

- Non-positive community id
- Empty name
- Whitespace-only slug
- Missing required request fields

Expected:

- Controlled 400 response
- No profile mutation

## Scope Verification

Confirm the implementation adds:

- No migration or model change
- No platform-admin endpoint change
- No teacher, student, grade, class, license, analytics, payment, dashboard, or member-management endpoint
- No new permissions framework or custom repository

See [data-model.md](./data-model.md) and [the API contract](./contracts/communities-api.openapi.yaml) for expected fields and authorization.
