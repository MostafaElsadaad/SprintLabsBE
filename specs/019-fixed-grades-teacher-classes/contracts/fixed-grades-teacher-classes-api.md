# HTTP Contract: Fixed Grades, Teacher Classes, and Rosters

Base path: `/api/v1/Communities`

All routes below use the existing authentication, `BaseResponse<T>`, controlled error, and API version conventions. For staff operations, the HTTP client never supplies `communityId`. The API resolves the current Community from the authenticated JWT `userId` and persisted Active staff membership, then handlers independently enforce role, membership, status, and resource ownership.

## List Fixed Grades

`GET /api/v1/Communities/grades`

Authorization: Active Community Owner or Teacher in exactly one current Active Community.

Request: no body; no community query/path value.

Success data (`200`):

```json
[
  { "id": 101, "value": 7 },
  { "id": 102, "value": 8 },
  { "id": 103, "value": 9 },
  { "id": 104, "value": 10 },
  { "id": 105, "value": 11 },
  { "id": 106, "value": 12 }
]
```

Rules:

- Exactly six supported rows, ordered numerically.
- Unsupported/unresolved legacy rows are never returned.
- Missing/ambiguous supported fixed data returns the existing controlled conflict response rather than an incomplete or arbitrarily selected list.
- `POST /api/v1/Communities/grades` is removed. No staff create/edit/delete/reorder Grade operation exists.

Common errors: unauthenticated, suspended/no valid staff context, forbidden membership, or fixed-grade data conflict using established status/envelope behavior.

## Replace a Teacher's Classes

`PUT /api/v1/Communities/teachers/{teacherUserId}/classes`

Authorization: Active Community Owner only.

Request:

```json
{
  "classIds": [201, 202, 203]
}
```

`classIds` is required. Duplicate IDs are normalized. `[]` removes every current-community assignment for the target Teacher.

Success data (`200`):

```json
{
  "userId": 501,
  "name": "Teacher One",
  "email": "teacher1.demo@sprintlabs.local",
  "status": "Active",
  "createdAt": "2026-08-30T10:00:00Z",
  "classes": [
    {
      "classId": 201,
      "className": "Class 7A",
      "gradeId": 101,
      "grade": 7
    }
  ]
}
```

Validation is completed before mutation:

- Target is an Active Teacher in the resolved current Community.
- Every Class is Active, in that Community, and linked to a supported Grade.
- Foreign, inactive, unsupported, missing, or mismatched resources use safe existing not-found/validation behavior and change no assignments.
- A concurrent unique-pair collision returns a controlled conflict; duplicate rows are never created.

Common errors: `400` malformed input, `401` unauthenticated, `403` non-Owner/suspended caller, `404` tenant-safe invalid Teacher/Class, `409` integrity race. Exact error bodies retain the existing `BaseResponse`/`GenericException` pipeline.

## List Teachers

`GET /api/v1/Communities/teachers`

Authorization: Active Community Owner only.

Query parameters:

| Name | Type | Required | Rules |
|---|---|---|---|
| `pageNumber` | integer | no | Default 1; must be positive. |
| `pageSize` | integer | no | Default 10; 1-100. |
| `gradeId` | long | no | Supported Grade in current Community. |
| `classId` | long | no | Active Class in current Community on a supported Grade. |
| `search` | string | no | Existing identity name/email search semantics. |
| `status` | CommunityUserStatus | no | Existing membership-status semantics. |

When both IDs are supplied, the Class must belong to the Grade. A Grade match is derived through an Active assigned Class; no TeacherGrade data exists.

Success data (`200`) uses the existing paged envelope shape:

```json
{
  "data": [
    {
      "userId": 501,
      "name": "Teacher One",
      "email": "teacher1.demo@sprintlabs.local",
      "status": "Active",
      "createdAt": "2026-08-30T10:00:00Z",
      "classes": [
        {
          "classId": 201,
          "className": "Class 7A",
          "gradeId": 101,
          "grade": 7
        }
      ]
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 1,
  "totalPages": 1
}
```

The displayed object above is the `PagedResponse<TeacherResponse>` data inside the established `BaseResponse` wrapper; wire casing follows current JSON configuration.

Rules:

- A Teacher appears once even when multiple assignments match a Grade.
- With no grade/class filters, existing roster status visibility is preserved and unassigned Teachers have `classes: []`.
- `classes` includes only Active same-community Classes linked to supported Grades.
- Foreign/stale/mismatched filters return no foreign data and follow tenant-safe error behavior.

## Existing Student Roster (Compatibility)

`GET /api/v1/Communities/students`

Authorization: Active Community Owner or Teacher.

Existing query parameters and `PagedResponse` remain: `pageNumber`, `pageSize`, optional `gradeId`, `classId`, `status`, `search`. Grade/class/combined filters remain tenant validated. Existing records on preserved legacy Grades remain inspectable; new StudentLicenses cannot be created or moved onto unsupported Grades.

`GET /api/v1/Communities/students/{playerProfileId}` remains unchanged.

## Existing Class and Student-License Writes (Stricter Validation)

Existing HTTP routes and request shapes remain unchanged. For every new or updated Class or StudentLicense, `gradeId` must identify a same-current-community Grade whose integer value is 7-12. An unsupported preserved legacy Grade produces the established validation/not-found error and is not usable for new state.

## Explicit Compatibility Exclusions

- `GET /api/v1/Communities/{communityId}` remains for existing Student/backward-compatible community-profile access.
- Platform Admin routes retain explicit `communityId`.
- Existing current-community resolution and handler authorization are not changed by TeacherClassAssignment.
