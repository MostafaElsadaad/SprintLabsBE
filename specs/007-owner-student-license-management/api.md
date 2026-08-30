# Owner Student License Management API

## Feature Summary

This feature lets active community Owners add, list, update, and revoke student licenses while enforcing community student capacity. Licenses reserve seats while Pending or Active and release seats when Revoked.

No student login activation, email sending, bulk import, dashboard, analytics, payments, parent accounts, teacher management, or grade/class management is included.

## Endpoints

| Method | Route | Required access |
|---|---|---|
| POST | `/api/v1/Communities/{communityId}/student-licenses` | Authenticated user with Active Owner membership |
| GET | `/api/v1/Communities/{communityId}/student-licenses` | Authenticated user with Active Owner membership |
| PATCH | `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` | Authenticated user with Active Owner membership |
| DELETE | `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` | Authenticated user with Active Owner membership |

All successful responses use the existing `BaseResponse<T>` envelope.

## POST Add Student License

Request:

```json
{
  "email": "student@example.com",
  "gradeId": 1,
  "classId": 1
}
```

Success:

```json
{
  "data": {
    "id": 1,
    "communityId": 10,
    "email": "student@example.com",
    "userId": null,
    "playerProfileId": null,
    "status": "Pending",
    "grade": { "id": 1, "name": "Grade 5" },
    "class": { "id": 1, "name": "Class A" },
    "emailChangeCount": 0,
    "assignedByUserId": 20,
    "assignedByName": "Owner Name",
    "activatedAt": null,
    "createdAt": "2026-06-27T10:15:00Z"
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Behavior:

- Normalizes email by trimming and lowercasing.
- Requires `UsedStudents < MaxStudents`.
- Requires grade and class to belong to the route community.
- Requires class to belong to the selected grade and be Active.
- Rejects duplicate non-revoked email in the same community.
- Creates a Pending license and increments `UsedStudents` when the email does not belong to an existing Google-registered user.
- If the normalized email belongs to an existing Google-registered user, creates an Active license immediately, links `UserId` and `PlayerProfileId`, sets `activatedAt`, creates or restores Student community access, and still increments `UsedStudents` only once.
- Does not create student community access for Pending licenses.

## GET List Student Licenses

Optional query parameters:

```http
GET /api/v1/Communities/{communityId}/student-licenses?status=Pending&gradeId=1&classId=1&search=student
```

Success returns `BaseResponse<List<StudentLicenseResponse>>`.

Filters:

- `status`: `Pending`, `Active`, or `Revoked`
- `gradeId`: same-community grade
- `classId`: same-community class
- `search`: email contains search text after normalization

## PATCH Update Student License

Request:

```json
{
  "email": "new-student@example.com",
  "gradeId": 1,
  "classId": 1
}
```

Behavior:

- Finds the license in the route community.
- Allows email changes only while status is Pending.
- Enforces `StudentEmailChangeLimit`.
- Increments `EmailChangeCount` only when normalized email changes.
- Rejects duplicate non-revoked email in the same community.
- Updates grade/class when the pair is valid.
- Does not change `UsedStudents`.

## DELETE Revoke Student License

No request body.

Behavior:

- Sets status to `Revoked`.
- Does not hard delete the license.
- Decrements `UsedStudents` only when previous status was Pending or Active.
- Already Revoked licenses do not decrement again.
- Active licenses with `UserId` mark the matching Student `CommunityUser` access as Removed.
- Does not touch unrelated memberships.

## Common Errors

| HTTP | Meaning |
|---|---|
| 400 | Invalid id/input, invalid email, full capacity, duplicate email, email change limit reached, deleted/mismatched class, or invalid status transition |
| 401 | Missing or invalid bearer token |
| 403 | Suspended user or authenticated user lacks Active Owner membership |
| 404 | Current user, license, capacity record, grade, or class not found in the route community |

Controlled errors use the existing `GenericException` and global error response style. Membership and role failures currently use `InvalidAccessToken`.

## Frontend Usage Notes

- Use `/api/v1/Users/me/communities` to determine which communities the user owns.
- Never send requester user id; the API reads it from JWT.
- Count Pending plus Active licenses as used seats.
- The add response may return `status: "Pending"` or `status: "Active"` depending on whether the target email already belongs to a Google-registered user.
- Treat Revoked licenses as historical records that do not grant access.
- Refresh license list and any capacity display after add or revoke.
- Refresh memberships if any endpoint returns 403.

## Manual Test Steps

Follow [quickstart.md](./quickstart.md).

## Open Questions

- None for this implementation.

## Implementation Notes / Spec Differences

- The implemented route is versioned and controller-based: `/api/v1/Communities/...`.
- Pending student license creation does not require a `User` row and does not create active `CommunityUser` access.
- Existing Google-registered students are activated immediately during add; not-yet-registered students remain Pending and are activated by login.

## Superseded Staff Contract

Student-license management routes no longer contain a CommunityId path parameter; the authenticated Owner membership resolves tenant context server-side. See [feature 018 API](../018-current-community-resolution/api.md).
