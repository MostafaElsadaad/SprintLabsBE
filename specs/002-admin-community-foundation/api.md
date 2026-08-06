# Admin Community Foundation API

Swagger groups these platform-admin endpoints under **Super Admin Control**. Their URLs remain `/api/v1/admin/communities`.

## Feature Summary

This feature lets authenticated platform admins create and list communities, assign community owners, and create or update license limits. Creating a community also sends its initial Community Admin a one-time password-setup invitation.

All endpoints require:

```http
Authorization: Bearer YOUR_SPRINTLABS_JWT
```

The JWT must contain a parseable `userId` claim. The corresponding user must exist, be active, and have `IsPlatformAdmin = true`.

Successful responses use:

```json
{
  "data": {},
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

## Endpoint List

| Method | Route | Permission | Purpose |
|---|---|---|---|
| POST | `/api/v1/admin/communities` | Platform admin | Create a community |
| GET | `/api/v1/admin/communities` | Platform admin | List communities with owner/license summaries |
| POST | `/api/v1/admin/communities/{communityId}/owner` | Platform admin | Assign or reactivate an owner membership |
| PATCH | `/api/v1/admin/communities/{communityId}/licenses` | Platform admin | Create or update license limits |

## POST /api/v1/admin/communities

### Request Body

```json
{
  "name": "Example School",
  "adminEmail": "owner@example.com"
}
```

Implemented validation:

- `name` and `adminEmail` must contain non-whitespace text.
- Both values are trimmed.
- The backend derives a lowercase URL-safe slug from the name.
- The normalized slug must be unique.
- Database limits are 200 characters for name and 120 for slug, but the handler does not prevalidate lengths.
- The backend creates a Pending Owner membership and emails a hashed-token setup link. The owner chooses their name and Identity password through the existing anonymous community-invitation completion endpoint; no password is accepted by this request.

### Success Response

```json
{
  "data": {
    "id": 10,
    "name": "Example School",
    "slug": "example-school",
    "status": "Active"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

### Common Errors

| HTTP | Condition | Typical message |
|---|---|---|
| 400 | Empty name/admin email or name that cannot produce a slug | `Invalid Data` |
| 400 | Duplicate normalized slug | `Record Already Exists` |
| 401 | Missing/invalid bearer token or `userId` claim | Authentication response may be empty |
| 403 | Missing, suspended, or non-platform-admin user | `InvalidAccessToken` |

The setup link points to `/invitations/community-admin/setup?token=...`. That page completes setup through the shared `POST /api/v1/Account/community-register` endpoint.

## POST /api/v1/Account/community-register

Completes the initial Community Admin setup from the emailed link. This is not public administrator creation: the token must identify an unexpired, unrevoked Pending Owner invitation created during trusted community provisioning.

### Request Body

```json
{
  "token": "invitation-token-from-link",
  "name": "Community Administrator",
  "password": "StrongPassword123!"
}
```

The request accepts no community ID, email, role, or administrator flag. The backend resolves the user and community exclusively from the hashed invitation token, validates the configured Identity password policy, creates the password through Identity, confirms the email, and activates the Owner membership.

### Success Response

Returns the standard HTTP 200 `BaseResponse` success envelope without a token. The user must then sign in through `POST /api/v1/Account/community-login`.

### Common Errors

| HTTP | Condition |
|---|---|
| 400 | Invalid, expired, revoked, used, or non-Owner invitation token; invalid password policy; invalid input. |

The same endpoint also completes Teacher invitations; the server resolves the role from the invitation token.

## GET /api/v1/admin/communities

Returns all communities ordered by `name`.

### Success Response

```json
{
  "data": [
    {
      "id": 10,
      "name": "Example School",
      "slug": "example-school",
      "status": "Active",
      "owner": {
        "userId": 42,
        "email": "owner@example.com",
        "name": "Owner Name",
        "status": "Active"
      },
      "license": {
        "maxStudents": 100,
        "usedStudents": 25,
        "maxTeachers": 10,
        "usedTeachers": 3,
        "studentEmailChangeLimit": 2
      }
    },
    {
      "id": 11,
      "name": "Unconfigured School",
      "slug": "unconfigured-school",
      "status": "Active",
      "owner": null,
      "license": null
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Only active Owner memberships are considered for the owner summary.

### Common Errors

| HTTP | Condition |
|---|---|
| 401 | Missing/invalid bearer token or `userId` claim |
| 403 | Current user is missing, suspended, or not a platform admin |

## POST /api/v1/admin/communities/{communityId}/owner

### Request Body

```json
{
  "email": "owner@example.com",
  "name": "Owner Name"
}
```

Implemented behavior:

- `communityId` must be greater than zero and identify an existing community.
- Email and name are trimmed and must be non-empty.
- Existing users are matched by normalized/case-insensitive email.
- If no user exists, an active non-admin user is created without a Google ID.
- The membership is created or updated to `Owner` and `Active`.
- Reassigning the same user does not create a duplicate community membership.
- Existing user profile/name data is not updated from this request.

### Success Response

```json
{
  "data": {
    "userId": 42,
    "email": "owner@example.com",
    "name": "Owner Name",
    "status": "Active",
    "communityUserId": 77,
    "communityId": 10,
    "role": "Owner"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

### Common Errors

| HTTP | Condition | Typical message |
|---|---|---|
| 400 | Invalid ID, empty email/name, or user creation failure | `Invalid Data` or `User Creating Failed` |
| 401 | Missing/invalid bearer token | Authentication response may be empty |
| 403 | Current user is not an active platform admin | `InvalidAccessToken` |
| 404 | Community does not exist | `Resource Not Found` |

The handler does not perform explicit email-format validation.

## PATCH /api/v1/admin/communities/{communityId}/licenses

### Request Body

```json
{
  "maxStudents": 100,
  "maxTeachers": 10,
  "studentEmailChangeLimit": 2
}
```

Implemented validation:

- `communityId` must be greater than zero and exist.
- All three request values must be zero or greater.
- `maxStudents` cannot be lower than current `usedStudents`.
- `maxTeachers` cannot be lower than current `usedTeachers`.
- Updating limits preserves both used counts.

### Success Response

```json
{
  "data": {
    "maxStudents": 100,
    "usedStudents": 25,
    "maxTeachers": 10,
    "usedTeachers": 3,
    "studentEmailChangeLimit": 2,
    "id": 14,
    "communityId": 10
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

For a newly created license, `usedStudents` and `usedTeachers` are both zero.

### Common Errors

| HTTP | Condition | Typical message |
|---|---|---|
| 400 | Negative value or maximum below current usage | `Invalid Data` |
| 401 | Missing/invalid bearer token | Authentication response may be empty |
| 403 | Current user is not an active platform admin | `InvalidAccessToken` |
| 404 | Community does not exist | `Resource Not Found` |

## Frontend Usage Notes

- Use `/Users/me` from the identity feature to decide whether to show admin-community UI.
- Still handle 403 on every admin request; frontend visibility is not authorization.
- Refresh the community list after create, owner assignment, or license update.
- Treat `owner` and `license` as nullable setup states.
- Display used counts as read-only. This API cannot change usage directly.
- Before submitting reduced maximums, compare against the latest used counts to avoid a predictable 400.

## Manual Test Steps

1. Sign in as a user with `isPlatformAdmin: true`.
2. Create a community and confirm the returned slug is trimmed/lowercase and status is `Active`.
3. Repeat the normalized slug and confirm a 400 response.
4. List communities and confirm the new row appears with nullable owner/license summaries.
5. Assign an owner using a new email; record the returned user and membership IDs.
6. Repeat the same owner assignment and confirm the membership ID is unchanged.
7. Create license limits and confirm used counts start at zero.
8. Update the limits and confirm used counts are preserved.
9. Attempt a negative limit and a maximum below used count; confirm 400.
10. Repeat an admin call with a non-admin JWT and confirm 403.
11. Repeat without a JWT and confirm 401.

## Implementation Notes / Spec Differences

- All create/update operations return HTTP 200; no endpoint returns 201.
- Duplicate slugs return 400 rather than 409.
- Admin authorization is a shared handler helper, not an ASP.NET authorization policy/filter.
- The implementation uses generic repositories; the custom community repository described in the original plan/tasks was removed.
- The schema prevents duplicate `(CommunityId, UserId)` memberships but does not enforce one Owner across all users in a community.
- Assigning an existing user does not update that user's name from the request.
- Admin-community task checkboxes are not updated even though the endpoints are implemented.

## Open Questions

- Is a community intended to have exactly one active owner? Current code can retain multiple active Owner memberships for different users.
- Should assigning an existing owner update their stored name?
- What slug character policy should the frontend enforce beyond lowercase/non-empty?
- Should duplicate slug use 409 Conflict instead of 400?
- Should email format and database length limits receive explicit API validation?
