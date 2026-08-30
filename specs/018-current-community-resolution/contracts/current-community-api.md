# Current-Community API Contract

**Feature**: [Trusted Current-Community Resolution](../spec.md)  
**Base route**: `/api/v1/Communities`  
**Authentication**: Bearer JWT with a signed, parseable `userId`

## Trust and Resolution Rules

- The client does not send `communityId` in any staff route path, query, or request body in this contract.
- The server resolves the current staff community from the signed `userId` and live `CommunityUser` rows.
- Owner and Teacher are staff roles.
- Pending and Active are current for conflict detection; only Active grants access.
- The resolved Community must be Active.
- Zero or multiple current staff communities fail closed; the server never chooses one by ordering.
- Resolution supplies tenant context only. Existing Owner-only and Owner-or-Teacher checks still run inside handlers.
- Response DTOs may continue returning CommunityId where it is already part of the response.

## Route Migration Matrix

| Method | Previous staff route | Required route | Permission |
|---|---|---|---|
| GET | `/Communities/{communityId}` | `/Communities/me` for staff; previous route retained for compatibility | `/me`: Active Owner or Teacher; retained route: Active Owner, Teacher, or Student in selected community |
| PATCH | `/Communities/{communityId}` | `/Communities/me` | Active Owner |
| GET | `/Communities/{communityId}/teachers` | `/Communities/teachers` | Active Owner |
| POST | `/Communities/teachers/invite` | unchanged | Active Owner |
| DELETE | `/Communities/{communityId}/teachers/{teacherUserId}` | `/Communities/teachers/{teacherUserId}` | Active Owner |
| POST | `/Communities/{communityId}/grades` | `/Communities/grades` | Active Owner or Teacher |
| GET | `/Communities/{communityId}/grades` | `/Communities/grades` | Active Owner or Teacher |
| POST | `/Communities/{communityId}/classes` | `/Communities/classes` | Active Owner or Teacher |
| GET | `/Communities/{communityId}/classes` | `/Communities/classes` | Active Owner or Teacher |
| PATCH | `/Communities/{communityId}/classes/{classId}` | `/Communities/classes/{classId}` | Active Owner or Teacher |
| DELETE | `/Communities/{communityId}/classes/{classId}` | `/Communities/classes/{classId}` | Active Owner or Teacher |
| POST | `/Communities/{communityId}/student-licenses` | `/Communities/student-licenses` | Active Owner |
| GET | `/Communities/{communityId}/student-licenses` | `/Communities/student-licenses` | Active Owner |
| PATCH | `/Communities/{communityId}/student-licenses/{licenseId}` | `/Communities/student-licenses/{licenseId}` | Active Owner |
| DELETE | `/Communities/{communityId}/student-licenses/{licenseId}` | `/Communities/student-licenses/{licenseId}` | Active Owner |
| GET | `/Communities/{communityId}/students` | `/Communities/students` | Active Owner or Teacher |
| GET | `/Communities/{communityId}/students/{playerProfileId}` | `/Communities/students/{playerProfileId}` | Active Owner or Teacher |

Except for retained `GET /Communities/{communityId}`, the previous numeric staff templates are removed and do not provide a fallback tenant-selection path.

## Common Response Envelope

Existing successful routes continue returning their established `BaseResponse<T>` shape:

```json
{
  "data": {},
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

This feature changes request tenant selection, not established data DTOs.

## Community Profile

### GET `/api/v1/Communities/me`

**Permission**: Active Owner or Teacher in the resolved current community.

**Request**: No path parameter, query parameter, or body.

**Response data**: Existing `CommunityProfileResponse`:

```json
{
  "id": 10,
  "name": "Example School",
  "slug": "example-school",
  "status": "Active"
}
```

### PATCH `/api/v1/Communities/me`

**Permission**: Active Owner in the resolved current community.

**Request body**:

```json
{
  "name": "Example School",
  "slug": "example-school"
}
```

Name/slug validation, normalization, uniqueness, response, and error behavior remain unchanged.

### Retained GET `/api/v1/Communities/{communityId}`

This existing endpoint remains available to an authenticated user with Active Owner, Teacher, or Student membership in the specified community. It is the compatibility exception and is not used for the new staff current-community flow.

## Teacher Management

### GET `/api/v1/Communities/teachers`

**Permission**: Active Owner.

**Request**: No path/query/body fields.

**Response data**: Existing list of `TeacherResponse` items with `userId`, `name`, `email`, `status`, and `createdAt`.

### POST `/api/v1/Communities/teachers/invite`

**Permission**: Active Owner.

**Request body**:

```json
{
  "email": "teacher@example.com"
}
```

This route was already community-ID-free. It now uses the same combined current staff resolver at the HTTP boundary. The invitation lifecycle and response remain unchanged.

### DELETE `/api/v1/Communities/teachers/{teacherUserId}`

**Permission**: Active Owner.

**Path fields**:

- `teacherUserId`: required positive target User ID.

The existing soft removal, invitation revocation, and teacher-seat release behavior remains unchanged.

## Grades

### POST `/api/v1/Communities/grades`

**Permission**: Active Owner or Teacher.

**Request body**:

```json
{
  "name": "Grade 5",
  "sortOrder": 5
}
```

### GET `/api/v1/Communities/grades`

**Permission**: Active Owner or Teacher.

**Request**: No path/query/body fields.

Create/list response fields and class-count behavior remain unchanged.

## Classes

### POST `/api/v1/Communities/classes`

**Permission**: Active Owner or Teacher.

**Request body**:

```json
{
  "gradeId": 3,
  "name": "Class A"
}
```

`gradeId` must belong to the resolved community.

### GET `/api/v1/Communities/classes`

**Permission**: Active Owner or Teacher.

**Query fields**:

- `gradeId` (optional): must belong to the resolved community.

### PATCH `/api/v1/Communities/classes/{classId}`

**Permission**: Active Owner or Teacher.

**Path fields**:

- `classId`: required positive Class ID owned by the resolved community.

**Request body**:

```json
{
  "name": "Class B",
  "gradeId": 4
}
```

Existing optional-field semantics remain; a supplied grade must belong to the resolved community.

### DELETE `/api/v1/Communities/classes/{classId}`

**Permission**: Active Owner or Teacher.

The existing soft-delete behavior remains unchanged.

## Student Licenses

### POST `/api/v1/Communities/student-licenses`

**Permission**: Active Owner.

**Request body**:

```json
{
  "email": "student@example.com",
  "gradeId": 3,
  "classId": 8
}
```

Grade/class ownership, capacity, email uniqueness, and seat reservation rules remain unchanged.

### GET `/api/v1/Communities/student-licenses`

**Permission**: Active Owner.

**Query fields**:

| Field | Required | Rule |
|---|---|---|
| `status` | No | Existing Pending/Active/Revoked filter. |
| `gradeId` | No | Must belong to the resolved community. |
| `classId` | No | Must belong to the resolved community. |
| `search` | No | Existing email search behavior. |

### PATCH `/api/v1/Communities/student-licenses/{licenseId}`

**Permission**: Active Owner.

**Path fields**:

- `licenseId`: required positive StudentLicense ID owned by the resolved community.

**Request body**:

```json
{
  "email": "student@example.com",
  "gradeId": 3,
  "classId": 8
}
```

Existing pending-only email-change, change-count, grade/class, and capacity behavior remains unchanged.

### DELETE `/api/v1/Communities/student-licenses/{licenseId}`

**Permission**: Active Owner.

Existing soft revocation, seat release, and matching Student-membership removal remain unchanged.

## Student Viewing

### GET `/api/v1/Communities/students`

**Permission**: Active Owner or Teacher.

**Query fields**:

| Field | Required | Default/Rule |
|---|---|---|
| `status` | No | Existing Pending/Active/Revoked filter. |
| `gradeId` | No | Must belong to resolved community. |
| `classId` | No | Must belong to resolved community. |
| `search` | No | Existing email/user/player search behavior. |
| `pageNumber` | No | Default `1`; must pass existing validation. |
| `pageSize` | No | Default `10`; must pass existing validation. |

Response remains `BaseResponse<PagedResponse<CommunityStudentListItemResponse>>`.

### GET `/api/v1/Communities/students/{playerProfileId}`

**Permission**: Active Owner or Teacher.

**Path fields**:

- `playerProfileId`: required positive PlayerProfile ID linked to a non-revoked StudentLicense in the resolved community.

Response remains `BaseResponse<CommunityStudentDetailResponse>` with existing placeholder analytics.

## Common Errors

| HTTP | Condition | Client behavior |
|---:|---|---|
| 400 | Invalid resource ID, request field, filter, paging, or grade/class relationship | Show existing validation/error state. |
| 401 | Missing/invalid bearer token or no parseable signed `userId` | End/repair local session. |
| 403 | No resolvable Active staff community; suspended user; wrong role; Pending/Removed/Student-only/no membership; Platform Admin without staff membership | Do not ask the user for a CommunityId; show unavailable/permission state. |
| 404 | Community-owned grade/class/license/student/teacher target not found in resolved tenant | Show existing not-found state without retrying another tenant. |
| 409 | Staff membership/invitation/owner-assignment conflict | Do not move or reassign automatically; preserve existing current relationship. |

Resolution failures never return the candidate or conflicting community identifiers.

## Explicit Platform Admin Exceptions

These existing routes are unchanged and continue requiring explicit CommunityId because a Platform Admin manages multiple communities:

| Method | Route |
|---|---|
| POST | `/api/v1/admin/communities/{communityId}/owner` |
| PATCH | `/api/v1/admin/communities/{communityId}/licenses` |

Platform Admin status alone does not authorize any route in the current-community staff contract.
