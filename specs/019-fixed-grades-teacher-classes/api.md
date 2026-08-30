# API Guide: Fixed Grades and Teacher Classes

Detailed wire examples are in [contracts/fixed-grades-teacher-classes-api.md](contracts/fixed-grades-teacher-classes-api.md). All staff calls use the authenticated user's server-resolved current Community. Do not send `communityId` in paths, query parameters, or bodies.

## Changed Endpoints

| Method | Endpoint | Roles | Request | Success | Common errors / frontend note |
|---|---|---|---|---|---|
| `GET` | `/api/v1/Communities/grades` | Active Owner or Teacher | None | Existing envelope containing six `{ id, value }` items ordered 7-12 | Unauthorized/forbidden/current-context failure; controlled conflict for invalid fixed-grade data. Use as read-only reference data. |
| `POST` | `/api/v1/Communities/grades` | None | Removed | No operation | Route is unavailable (normal route-not-found/method behavior). Remove all frontend callers and mutation UI. |
| `GET` | `/api/v1/Communities/teachers` | Active Owner | `pageNumber`, `pageSize`, optional `gradeId`, `classId`, `search`, `status` | Existing envelope containing `PagedResponse<TeacherResponse>` | `400` invalid paging; `403` not Owner; tenant-safe error for invalid filters. Do not page/filter locally. |
| `PUT` | `/api/v1/Communities/teachers/{teacherUserId}/classes` | Active Owner | `{ "classIds": [1,2] }` | Updated Teacher response including complete Active class set | `400` malformed; `403` not Owner; tenant-safe invalid Teacher/Class; `409` concurrent duplicate conflict. Submit `[]` to clear. |

## Grade Response

```json
{
  "id": 101,
  "value": 7
}
```

Only values 7-12 are returned. Preserved unsupported legacy rows are excluded and cannot be selected for new data.

## Teacher Query and Response

Defaults are `pageNumber=1`, `pageSize=10`; maximum page size is 100. `classId` means assigned to that exact Active Class. `gradeId` means assigned to at least one Active Class in that Grade. When both are present, the Class must belong to the Grade.

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

Teachers with no assignments are returned with `classes: []` when no class/grade filter excludes them. Assignment does not grant API authorization.

## Unchanged Endpoints with Stricter Grade Writes

The current community-scoped (server-owned) Class and StudentLicense endpoints keep their current routes, bodies, roles, envelopes, and errors. Whenever they create or update a grade reference, `gradeId` must now be a same-current-community fixed Grade (7-12). A preserved unsupported legacy Grade is read-only remediation data and is rejected for new state.

## Student APIs (Verified Compatibility)

| Method | Endpoint | Roles | Request | Success / notes |
|---|---|---|---|---|
| `GET` | `/api/v1/Communities/students` | Active Owner or Teacher | Existing `pageNumber`, `pageSize`, `gradeId`, `classId`, `status`, `search` | Existing `PagedResponse`; database paging/filtering and tenant isolation remain. |
| `GET` | `/api/v1/Communities/students/{playerProfileId}` | Active Owner or Teacher | Path ID | Existing detail response remains. |

Existing legacy-linked students remain readable. Fixed supported grades are rendered numerically within the existing response fields; there is no StudentGrade model.

## Compatibility Routes Not Changed

- `GET /api/v1/Communities/{communityId}` remains available for Student/backward-compatible community-profile access.
- Platform Admin endpoints continue accepting explicit `communityId` because admins select a target Community.
- Current JWT `userId` -> active staff membership -> active Community resolution remains the staff tenant boundary.
