# Community Grades and Classes API

## Feature Summary

This feature lets active community Owners and Teachers manage grades and classes for a selected community. Classes are soft deleted with `status = "Deleted"` and are excluded from active class lists and grade class counts.

No student assignment, student management, student licenses, analytics, payments, imports, dashboard, or grade update/delete endpoints are included.

## Endpoints

| Method | Route | Required access |
|---|---|---|
| POST | `/api/v1/Communities/{communityId}/grades` | Authenticated user with Active Owner or Teacher membership |
| GET | `/api/v1/Communities/{communityId}/grades` | Authenticated user with Active Owner or Teacher membership |
| POST | `/api/v1/Communities/{communityId}/classes` | Authenticated user with Active Owner or Teacher membership |
| GET | `/api/v1/Communities/{communityId}/classes?gradeId={gradeId}` | Authenticated user with Active Owner or Teacher membership |
| PATCH | `/api/v1/Communities/{communityId}/classes/{classId}` | Authenticated user with Active Owner or Teacher membership |
| DELETE | `/api/v1/Communities/{communityId}/classes/{classId}` | Authenticated user with Active Owner or Teacher membership |

All successful responses use the existing `BaseResponse<T>` envelope.

## POST Create Grade

Request:

```json
{
  "name": "Grade 5",
  "sortOrder": 5
}
```

Success:

```json
{
  "data": {
    "id": 1,
    "communityId": 10,
    "name": "Grade 5",
    "sortOrder": 5,
    "classCount": 0,
    "createdAt": "2026-06-27T10:15:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`name` is required, trimmed, and limited to 120 characters. `sortOrder` must be zero or greater.

## GET List Grades

No request body.

Success:

```json
{
  "data": [
    {
      "id": 1,
      "communityId": 10,
      "name": "Grade 5",
      "sortOrder": 5,
      "classCount": 2,
      "createdAt": "2026-06-27T10:15:00Z"
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Grades are scoped to the route community and ordered by `sortOrder`, then `name`. `classCount` counts only classes with `status = "Active"`.

## POST Create Class

Request:

```json
{
  "gradeId": 1,
  "name": "Class A"
}
```

Success:

```json
{
  "data": {
    "id": 5,
    "communityId": 10,
    "gradeId": 1,
    "name": "Class A",
    "status": "Active",
    "createdAt": "2026-06-27T10:20:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

`gradeId` must belong to the same route community.

## GET List Classes

Optional query string:

```http
GET /api/v1/Communities/10/classes?gradeId=1
```

Success:

```json
{
  "data": [
    {
      "id": 5,
      "communityId": 10,
      "gradeId": 1,
      "name": "Class A",
      "status": "Active",
      "createdAt": "2026-06-27T10:20:00Z"
    }
  ],
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Only active classes for the route community are returned. If `gradeId` is supplied, it must belong to the same route community.

## PATCH Update Class

Request:

```json
{
  "name": "Updated Class A",
  "gradeId": 2
}
```

`name` is required. `gradeId` is optional; omit it to rename without moving the class. When supplied, the grade must belong to the same route community.

Success returns the updated class response.

## DELETE Class

No request body.

Behavior:

- Sets `status = "Deleted"`.
- Does not hard delete the row.
- Repeating delete on an already deleted class returns the deleted class response.
- Deleted classes no longer appear in list classes or grade class counts.

## Common Errors

| HTTP | Meaning |
|---|---|
| 400 | Invalid id/input, empty name, negative sort order, too-long name, or update of deleted class |
| 401 | Missing or invalid bearer token |
| 403 | Suspended user or authenticated user lacks Active Owner/Teacher membership |
| 404 | Current user, grade, or class not found in the route community |

Controlled errors use the existing `GenericException` and global error response style. Membership and role failures currently use the existing `InvalidAccessToken` message.

## Frontend Usage Notes

- Use `GET /api/v1/Users/me/communities` to choose a community where the user has role `Owner` or `Teacher`.
- Never send the requester user id; the API reads it from the JWT.
- Treat `status = "Deleted"` as inactive and remove the row from active class views.
- Refresh grade lists after class create, move, or delete because class counts can change.
- Keep client-side validation aligned with server limits: grade/class names required and max 120 characters, `sortOrder >= 0`.

## Manual Test Steps

1. Authenticate as an Active Owner or Teacher for a community.
2. Create a grade with `POST /api/v1/Communities/{communityId}/grades`.
3. List grades and confirm the new grade is returned with `classCount = 0`.
4. Create a class under that grade.
5. List classes with and without `gradeId`.
6. Update the class name, then move it to another same-community grade.
7. Delete the class and confirm it is absent from class lists and grade counts.
8. Repeat create/list/update/delete as a Student or inactive member and confirm 403.

## Open Questions

- None for this implementation.

## Implementation Notes / Spec Differences

- The implemented route is versioned and controller-based: `/api/v1/Communities/...`.
- `PATCH` requires `name`; `gradeId` is optional so rename-only updates are supported.
- Platform-admin status does not bypass the Active Owner/Teacher membership requirement.

## Superseded Staff Contract

Grade and class routes no longer contain a CommunityId path parameter; the authenticated staff membership resolves tenant context server-side. See [feature 018 API](../018-current-community-resolution/api.md).
