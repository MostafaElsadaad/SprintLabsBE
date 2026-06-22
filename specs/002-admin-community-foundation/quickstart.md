# Quickstart: Admin Community Foundation

## Prerequisites

- User Identity Foundation migrations are applied.
- At least one user has `IsPlatformAdmin = true`.
- A valid JWT exists for that platform admin and includes the existing `userId` claim.
- MySQL connection string is configured for the API.

## Build and Migration Checks

```powershell
dotnet build SprintLabs.sln
```

Expected outcome: solution builds successfully.

Implementation should also create and apply an EF Core migration for:

- `Communities`
- `CommunityUsers`
- `CommunityLicenses`
- unique index on `Community.Slug`
- unique index on `CommunityUser` for `CommunityId + UserId`
- unique index on `CommunityLicense.CommunityId`

## API Validation Scenarios

Use the admin JWT as:

```text
Authorization: Bearer <platform-admin-token>
```

### 1. Unauthenticated access is rejected

Request any admin community endpoint without a token.

Expected outcome: request returns 401 and no data changes.

### 2. Non-admin access is rejected

Request any admin community endpoint with a valid non-admin user token.

Expected outcome: request returns 403 or the project's existing controlled forbidden equivalent and no data changes.

### 3. Create community

```http
POST /api/v1/admin/communities
Content-Type: application/json

{
  "name": "Example School",
  "slug": "example-school"
}
```

Expected outcome: response contains the created community with `status = Active`.

### 4. Reject duplicate slug

Repeat the create-community request with the same slug.

Expected outcome: duplicate is rejected and only one community exists for that slug.

### 5. List communities

```http
GET /api/v1/admin/communities
```

Expected outcome: response contains all communities, including owner summary and license summary when configured.

### 6. Assign owner

```http
POST /api/v1/admin/communities/{communityId}/owner
Content-Type: application/json

{
  "email": "owner@example.com",
  "name": "Owner Name"
}
```

Expected outcome: response contains owner info. If no user existed for the email, a basic active non-admin user is created without GoogleId.

Repeat the same request.

Expected outcome: no duplicate `CommunityUser` row is created for the same community/user pair.

### 7. Create or update license limits

```http
PATCH /api/v1/admin/communities/{communityId}/licenses
Content-Type: application/json

{
  "maxStudents": 100,
  "maxTeachers": 10,
  "studentEmailChangeLimit": 2
}
```

Expected outcome: license is created if missing, or updated if present. `UsedStudents` and `UsedTeachers` remain unchanged except initial creation sets both to zero.

### 8. Reject limits below usage

After a license has non-zero usage, submit limits lower than current usage.

Expected outcome: request is rejected and existing limits/usage remain unchanged.
