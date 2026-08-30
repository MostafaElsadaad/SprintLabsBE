# Owner Community Profile API

## Feature Summary

This feature exposes basic community profile viewing to active members and profile editing to active Owners. Authorization is membership-based and does not grant a platform-admin bypass.

## Endpoints

| Method | Route | Required access |
|---|---|---|
| GET | `/api/v1/Communities/{communityId}` | Authenticated user with Active community membership |
| PATCH | `/api/v1/Communities/{communityId}` | Authenticated user with Active Owner membership |

Both successful responses use `BaseResponse<CommunityProfileResponse>`.

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

## GET Community Profile

No request body.

Success data:

```json
{
  "id": 10,
  "name": "Example School",
  "slug": "example-school",
  "status": "Active"
}
```

Active Owner, Teacher, and Student memberships may view. Pending, Removed, missing membership, and platform-admin-only access are denied.

The handler also rejects a missing user with 404 and a suspended user with 403 before checking membership.

## PATCH Community Profile

Request:

```json
{
  "name": "Updated School Name",
  "slug": "updated-school-slug"
}
```

Both fields are required. Values are trimmed and the slug is lowercased. The current community may retain its slug, but another community's slug is rejected.

Success returns the same four profile fields with updated values.

Implemented limits match the existing schema:

- Name: maximum 200 characters.
- Slug: maximum 120 characters.
- Status is not editable.
- A successful update sets `UpdatedAt`.

## Common Errors

| HTTP | Meaning |
|---|---|
| 400 | Invalid id/input or duplicate slug |
| 401 | Missing or invalid bearer token |
| 403 | Suspended user, inactive/missing membership, or non-Owner update |
| 404 | Current user or accessible community not found |

Controlled errors use the existing `BaseResponse` error shape. Membership and role failures currently use the existing `InvalidAccessToken` message.

## Frontend Usage Notes

- Use the selected membership's `communityId`; never submit a user id.
- Show profile editing only for role `Owner`, but still handle server-side 403.
- Treat status as read-only.
- Submit both name and slug for PATCH.
- Preserve entered values after validation errors.

## Manual Test Steps

Follow [quickstart.md](./quickstart.md).

## Open Questions

- None required for planning. The specification defines both PATCH fields as required and keeps suspended-community access membership-based.

## Implementation Notes

- The implemented project route is versioned as `/api/v1/Communities/{communityId}`.
- Authorization is evaluated in handlers through `ICommunityAccessService`; no policy or role attribute was added.
- Slug uniqueness is checked before save and remains protected by the existing unique database index.
- No migration or platform-admin endpoint change was required.

## Superseded Staff Contract

Feature 018 replaces staff profile calls with `GET` and `PATCH /api/v1/Communities/me`; clients no longer supply CommunityId. `GET /api/v1/Communities/{communityId}` remains for active Student-compatible profile access. See [feature 018 API](../018-current-community-resolution/api.md).
