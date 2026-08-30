# Trusted Current-Community Resolution API

## Feature Summary

Community Owners and Teachers no longer select a tenant by sending CommunityId to staff APIs. The backend reads the signed `userId`, resolves the user's one current Active Owner/Teacher membership in an Active Community from persisted data, and sends that server-owned CommunityId into the existing authorization handlers.

The complete request and response contract is in [contracts/current-community-api.md](./contracts/current-community-api.md).

## Authorization Model

- `Owner` and `Teacher` are staff roles.
- `Pending` and `Active` memberships count when detecting more than one current staff community.
- Only an `Active` membership in an `Active` Community can resolve.
- `Removed` and `Student` memberships are ignored by staff resolution.
- Zero or multiple current staff communities produce a safe authorization failure; the API never chooses one.
- Resolution does not replace existing role checks.
- Platform Admin status alone does not grant Communities route access.
- No CommunityId or community role is added to JWTs.

## Exact Route Migration

| Method | Old staff route | New staff route | Auth/role |
|---|---|---|---|
| GET | `/api/v1/Communities/{communityId}` | `/api/v1/Communities/me` for staff; old GET retained | `/me`: Owner/Teacher; old GET: Owner/Teacher/Student membership |
| PATCH | `/api/v1/Communities/{communityId}` | `/api/v1/Communities/me` | Owner |
| GET | `/api/v1/Communities/{communityId}/teachers` | `/api/v1/Communities/teachers` | Owner |
| POST | `/api/v1/Communities/teachers/invite` | unchanged | Owner |
| DELETE | `/api/v1/Communities/{communityId}/teachers/{teacherUserId}` | `/api/v1/Communities/teachers/{teacherUserId}` | Owner |
| POST | `/api/v1/Communities/{communityId}/grades` | `/api/v1/Communities/grades` | Owner/Teacher |
| GET | `/api/v1/Communities/{communityId}/grades` | `/api/v1/Communities/grades` | Owner/Teacher |
| POST | `/api/v1/Communities/{communityId}/classes` | `/api/v1/Communities/classes` | Owner/Teacher |
| GET | `/api/v1/Communities/{communityId}/classes` | `/api/v1/Communities/classes` | Owner/Teacher |
| PATCH | `/api/v1/Communities/{communityId}/classes/{classId}` | `/api/v1/Communities/classes/{classId}` | Owner/Teacher |
| DELETE | `/api/v1/Communities/{communityId}/classes/{classId}` | `/api/v1/Communities/classes/{classId}` | Owner/Teacher |
| POST | `/api/v1/Communities/{communityId}/student-licenses` | `/api/v1/Communities/student-licenses` | Owner |
| GET | `/api/v1/Communities/{communityId}/student-licenses` | `/api/v1/Communities/student-licenses` | Owner |
| PATCH | `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `/api/v1/Communities/student-licenses/{licenseId}` | Owner |
| DELETE | `/api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `/api/v1/Communities/student-licenses/{licenseId}` | Owner |
| GET | `/api/v1/Communities/{communityId}/students` | `/api/v1/Communities/students` | Owner/Teacher |
| GET | `/api/v1/Communities/{communityId}/students/{playerProfileId}` | `/api/v1/Communities/students/{playerProfileId}` | Owner/Teacher |

Except for retained profile GET, the old numeric staff templates are removed.

## Request Contracts

CommunityId is absent from every new staff path, query, and request body.

| Route | Body/query retained |
|---|---|
| `PATCH Communities/me` | Body: `name`, `slug` |
| `POST Communities/teachers/invite` | Body: `email` |
| `POST Communities/grades` | Body: `name`, `sortOrder` |
| `POST Communities/classes` | Body: `gradeId`, `name` |
| `GET Communities/classes` | Optional query: `gradeId` |
| `PATCH Communities/classes/{classId}` | Body: `name`, optional `gradeId` using existing semantics |
| `POST Communities/student-licenses` | Body: `email`, `gradeId`, `classId` |
| `GET Communities/student-licenses` | Optional query: `status`, `gradeId`, `classId`, `search` |
| `PATCH Communities/student-licenses/{licenseId}` | Body: `email`, `gradeId`, `classId` |
| `GET Communities/students` | Optional query: `status`, `gradeId`, `classId`, `search`, `pageNumber=1`, `pageSize=10` |

Resource IDs such as `teacherUserId`, `classId`, `licenseId`, `gradeId`, and `playerProfileId` remain and must belong to the resolved community.

## Success Responses

All routes retain their existing `BaseResponse<T>` envelopes and DTOs. Community/profile, teacher, grade, class, student-license, student-list, and student-detail response fields do not change. Server-returned CommunityId fields may remain.

## Common Errors

| HTTP | Common condition |
|---:|---|
| 400 | Existing request/filter/resource relationship validation failure. |
| 401 | Missing/invalid bearer token or signed `userId`. |
| 403 | No valid current staff community, suspended user, or role/membership denial. |
| 404 | Target resource is absent from the resolved community. |
| 409 | Pending/Active Owner/Teacher membership conflict in another community. |

Failures do not return conflicting Community IDs and never retry against another tenant.

## Compatibility Endpoints

### Student/backward-compatible profile read

`GET /api/v1/Communities/{communityId}` remains. Its current Active Owner, Teacher, and Student membership authorization remains unchanged.

### Platform Admin explicit scoping

These routes remain unchanged:

- `POST /api/v1/admin/communities/{communityId}/owner`
- `PATCH /api/v1/admin/communities/{communityId}/licenses`

They intentionally accept CommunityId because Platform Admin manages multiple communities.

## Frontend Usage Notes

- Remove CommunityId from every migrated request URL/body/query.
- Do not derive or cache a current tenant from login response data for these operations.
- Do not show an Owner/Teacher community switcher for these staff workflows.
- Continue sending resource IDs and supported filters.
- Continue treating authorization responses as authoritative even when UI visibility is role-based.
- Student clients using the retained profile GET continue sending the selected CommunityId.
- Platform Admin clients continue sending CommunityId on Admin routes.

## Documentation Supersession

For route shape only, this file supersedes the staff endpoint tables in specs 004, 005, 006, 007, 009, and the stale invitation route in spec 015. Feature 016 and the current implementation already use community-ID-free `POST /Communities/teachers/invite`.
