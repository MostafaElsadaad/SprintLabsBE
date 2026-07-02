# Community Student Viewing API

## Feature Summary

This feature adds read-only community student viewing for authenticated community Owners and Teachers. Student roster data is sourced from `StudentLicenses`, so Pending students can appear before login activation.

No student license creation, update, revoke, activation, assignments, reports, dashboards, exports, imports, database tables, migrations, or real analytics calculations are included.

## Endpoint List

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/v1/Communities/{communityId}/students` | Active Owner or Teacher in community | Paginated student roster |
| GET | `/api/v1/Communities/{communityId}/students/{playerProfileId}` | Active Owner or Teacher in community | Student detail by player profile |

## GET /api/v1/Communities/{communityId}/students

### Auth/Role Requirements

- Requires bearer token.
- Requires Active `Owner` or Active `Teacher` membership in the route community.
- Student, Pending, Removed, no-membership, and platform-admin-only users are denied.

### Query Parameters

| Name | Required | Notes |
|---|---|---|
| `gradeId` | No | Must belong to the route community when provided. |
| `classId` | No | Must belong to the route community and be active when provided. |
| `status` | No | Student license status: `Pending`, `Active`, or `Revoked`. |
| `search` | No | Matches license email, linked user email/name, or linked player email/name where available. |
| `pageNumber` | No | Existing `PagedRequest` field. Defaults to `1`. |
| `pageSize` | No | Existing `PagedRequest` field. Defaults to `10`. |

If both `gradeId` and `classId` are provided, the class must belong to the grade.

### Success Response

Returns `BaseResponse<PagedResponse<CommunityStudentListItemResponse>>`.

```json
{
  "data": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1,
    "totalRecords": 1,
    "data": [
      {
        "licenseId": 12,
        "email": "student@example.com",
        "status": "Active",
        "userId": 42,
        "playerProfileId": 99,
        "playerName": "Student Name",
        "avatarUrl": "https://example.com/avatar.png",
        "gradeId": 3,
        "gradeName": "Grade 5",
        "classId": 8,
        "className": "Class A",
        "activatedAt": "2026-07-02T11:00:00Z",
        "createdAt": "2026-07-01T10:00:00Z"
      }
    ]
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Pending students may return `null` for `userId`, `playerProfileId`, `playerName`, `avatarUrl`, and `activatedAt`.

## GET /api/v1/Communities/{communityId}/students/{playerProfileId}

### Auth/Role Requirements

- Requires bearer token.
- Requires Active `Owner` or Active `Teacher` membership in the route community.

### Success Response

Returns `BaseResponse<CommunityStudentDetailResponse>`.

```json
{
  "data": {
    "userId": 42,
    "playerProfileId": 99,
    "name": "Student Name",
    "email": "student@example.com",
    "avatarUrl": "https://example.com/avatar.png",
    "gold": 0,
    "experience": 0,
    "level": 1,
    "licenseStatus": "Active",
    "gradeId": 3,
    "gradeName": "Grade 5",
    "classId": 8,
    "className": "Class A",
    "activatedAt": "2026-07-02T11:00:00Z",
    "createdAt": "2026-07-01T10:00:00Z",
    "analytics": {
      "completedAssignments": 0,
      "averageScore": null,
      "lastActivityAt": null,
      "matchesPlayed": 0,
      "winRate": null
    }
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

The detail endpoint only returns a student when a non-revoked student license in the route community is linked to the requested `playerProfileId`.

## Common Error Responses

| HTTP | Condition |
|---|---|
| 400 | Invalid route id, page number, or page size. |
| 401 | Missing or invalid bearer token. |
| 403 | Authenticated user is not an Active Owner or Active Teacher for the community. |
| 404 | Grade/class filter is invalid, or student detail is not linked to a non-revoked license in the route community. |

## Frontend Usage Notes

- Use the roster endpoint for tables and student search screens.
- Refresh list data after student license management or student login activation flows.
- Treat analytics fields as placeholders only.
- Do not infer access from email or platform admin status; render based on server authorization and responses.
