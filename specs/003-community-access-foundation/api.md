# Community Access Foundation API

## Feature Summary

This feature provides reusable backend checks for active community membership and community roles, plus an authenticated endpoint that lists the current user's active communities. Platform-admin status remains separate from community membership.

No database migration or new community-management endpoint belongs to this feature.

## Endpoint List

| Method | Route | Authentication | Purpose |
|---|---|---|---|
| GET | `/api/v1/Users/me/communities` | Bearer JWT | Return the current user's active community memberships |

The reusable `CanAccessCommunity` and `HasCommunityRole` methods are internal backend services and are not HTTP endpoints.

## GET /api/v1/Users/me/communities

### Authentication and Permissions

Send:

```http
Authorization: Bearer YOUR_SPRINTLABS_JWT
```

The JWT must contain a parseable `userId` claim. Any active authenticated user can call the endpoint; platform-admin status is not required.

Only memberships satisfying both conditions are returned:

- `CommunityUser.UserId` equals the authenticated user ID.
- `CommunityUser.Status` is `Active`.

Owner, Teacher, and Student roles are all returned when active. Pending and Removed memberships are excluded.

### Request

No request body, path parameter, or query parameter.

### Success Response

```json
{
  "data": [
    {
      "communityId": 10,
      "communityName": "Example School",
      "slug": "example-school",
      "communityStatus": "Active",
      "role": "Owner",
      "membershipStatus": "Active"
    },
    {
      "communityId": 15,
      "communityName": "Science Club",
      "slug": "science-club",
      "communityStatus": "Suspended",
      "role": "Teacher",
      "membershipStatus": "Active"
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

When the user has no active memberships, `data` is an empty array.

Communities are ordered by community name. A suspended community can still appear because this endpoint filters membership status, not community status.

### Common Errors

| HTTP | Condition | Response behavior |
|---|---|---|
| 401 | Missing/invalid bearer token, or no parseable `userId` claim | Authentication response may be empty |
| 403 | Current user is suspended | `InvalidAccessToken` controlled error |
| 404 | JWT references a user that no longer exists | `Resource Not Found` controlled error |

Example controlled error:

```json
{
  "message": "InvalidAccessToken",
  "statusCode": 403,
  "errorCode": 3
}
```

## Reusable Backend Access Rules

These methods are available through `ICommunityAccessService` for future backend endpoints:

### CanAccessCommunity(userId, communityId)

Returns `true` only when an exact membership exists for that user/community with status `Active`.

### HasCommunityRole(userId, communityId, roles)

Returns `true` only when:

- The required role collection is not null or empty.
- The exact user/community membership is `Active`.
- Its role is one of `Owner`, `Teacher`, or `Student` supplied by the caller.

Platform-admin status does not bypass either check.

## Frontend Usage Notes

- Use this endpoint to populate a community switcher or community-aware navigation.
- Use `communityId` as the selected community context key.
- Use `role` to decide which future community experiences might be shown, but continue to handle authorization failures from those endpoints.
- Do not treat a suspended community as active merely because the membership is active.
- Do not expect Pending invitations in this response.
- Do not use `isPlatformAdmin` as a substitute for membership.

## Manual Test Steps

1. Sign in and obtain a JWT containing `userId`.
2. Seed or assign Active Owner, Teacher, and Student memberships for that user.
3. Seed Pending and Removed memberships for the same user.
4. Seed an Active membership for a different user.
5. Call `GET /api/v1/Users/me/communities`.
6. Confirm only the current user's Active memberships are returned.
7. Confirm each item contains `communityId`, `communityName`, `slug`, `communityStatus`, `role`, and `membershipStatus`.
8. Confirm Pending, Removed, and other-user memberships are absent.
9. Confirm an active membership in a suspended community is returned with `communityStatus: "Suspended"`.
10. Call without a JWT and confirm 401.
11. Where service-level tests are available, verify empty role sets deny access and platform admin does not bypass membership.

## Implementation Notes / Spec Differences

- The implemented response property is `communityName`; the feature OpenAPI contract currently declares `name`.
- The feature provides a reusable service only. It does not add Owner/Teacher/Student authorization attributes or filters.
- Access-service checks require active membership but do not check `Community.Status`.
- The current-user endpoint separately verifies that the user exists and is not suspended.
- The implementation uses `IBaseRepository<CommunityUser>` rather than the custom community repository described in the original plan/tasks.

## Open Questions

- Should suspended communities be excluded from the list, shown as disabled, or remain selectable?
- Should future community endpoints combine active membership checks with an active-community check by default?
- Should the OpenAPI contract be corrected from `name` to `communityName`, or should the DTO be renamed in a future API change?
- Are reusable role attributes/filters required before the first role-protected community endpoint is added?
