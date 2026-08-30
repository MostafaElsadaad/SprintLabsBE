# Frontend Handoff: Trusted Current-Community Resolution

## Migration Goal

Owner and Teacher screens operate on the authenticated user's one current staff community. Remove CommunityId from staff API URLs, request bodies, query parameters, stored current-community selection, and tenant-selection UI for the workflows below.

The backend remains authoritative. Do not add CommunityId to a JWT decoder, token model, or request interceptor.

## Exact Call Migration

| Screen/action | Old call | New call |
|---|---|---|
| Staff community header/profile load | `GET /api/v1/Communities/{communityId}` | `GET /api/v1/Communities/me` |
| Owner edits community profile | `PATCH /api/v1/Communities/{communityId}` | `PATCH /api/v1/Communities/me` |
| Owner lists teachers | `GET /api/v1/Communities/{communityId}/teachers` | `GET /api/v1/Communities/teachers` |
| Owner invites teacher | `POST /api/v1/Communities/teachers/invite` | unchanged; never add CommunityId |
| Owner removes teacher | `DELETE /api/v1/Communities/{communityId}/teachers/{teacherUserId}` | `DELETE /api/v1/Communities/teachers/{teacherUserId}` |
| Staff creates grade | `POST /api/v1/Communities/{communityId}/grades` | `POST /api/v1/Communities/grades` |
| Staff lists grades | `GET /api/v1/Communities/{communityId}/grades` | `GET /api/v1/Communities/grades` |
| Staff creates class | `POST /api/v1/Communities/{communityId}/classes` | `POST /api/v1/Communities/classes` |
| Staff lists classes | `GET /api/v1/Communities/{communityId}/classes` | `GET /api/v1/Communities/classes` |
| Staff edits class | `PATCH /api/v1/Communities/{communityId}/classes/{classId}` | `PATCH /api/v1/Communities/classes/{classId}` |
| Staff deletes class | `DELETE /api/v1/Communities/{communityId}/classes/{classId}` | `DELETE /api/v1/Communities/classes/{classId}` |
| Owner adds student license | `POST /api/v1/Communities/{communityId}/student-licenses` | `POST /api/v1/Communities/student-licenses` |
| Owner lists student licenses | `GET /api/v1/Communities/{communityId}/student-licenses` | `GET /api/v1/Communities/student-licenses` |
| Owner edits student license | `PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `PATCH /api/v1/Communities/student-licenses/{licenseId}` |
| Owner revokes student license | `DELETE /api/v1/Communities/{communityId}/student-licenses/{licenseId}` | `DELETE /api/v1/Communities/student-licenses/{licenseId}` |
| Staff lists students | `GET /api/v1/Communities/{communityId}/students` | `GET /api/v1/Communities/students` |
| Staff opens student detail | `GET /api/v1/Communities/{communityId}/students/{playerProfileId}` | `GET /api/v1/Communities/students/{playerProfileId}` |

Do not keep old numeric routes as fallback. The only retained numeric Communities route is the Student/backward-compatible profile read described below.

## Shared Staff Shell / Community Header

**Visibility**: Authenticated Community Owner or Teacher.

**Load**: `GET /api/v1/Communities/me`.

**Display fields**:

- community name
- slug where currently shown
- status where currently shown

**Changes**:

- Remove community picker/switcher behavior for these staff workflows.
- Remove reliance on a CommunityId stored in route state, browser storage, app state, or login response.
- A server-returned CommunityId may still be displayed/retained as response data, but it must not be used to select the tenant on migrated calls.

**States**:

- Loading: show the existing community-shell loading state while `/me` resolves.
- Empty/403: show that no current staff community is available or access is unavailable; do not prompt for a CommunityId.
- 401: clear/end the invalid session according to existing authentication behavior.
- Other error: use the existing retry/error surface without changing tenants.

## Owner Community Profile

**Visibility**: Edit controls visible to Community Owner only. Teachers may load `/me` but cannot edit.

**Form fields**:

- `name`: required, existing max-length/trim behavior
- `slug`: required, existing normalization/uniqueness behavior

**Save**: `PATCH /api/v1/Communities/me` with only `name` and `slug`.

**States**:

- Preserve existing submitting, validation, duplicate-slug, success, and error behavior.
- A 403 after page load means current role/membership changed; hide/disable edit flow after showing the error.

## Teacher Management Section

**Visibility**: Community Owner only.

### Teacher table

**Load**: `GET /api/v1/Communities/teachers`.

**Columns**:

- name
- email
- membership status
- created date
- remove action

Use the existing `userId` response as `teacherUserId` for the remove action.

### Invite form

**Fields**:

- `email`: required valid email

**Action**: `POST /api/v1/Communities/teachers/invite` with `{ "email": "..." }`.

This route was already community-ID-free. Do not reintroduce CommunityId in a shared request wrapper.

### Remove action

**Action**: `DELETE /api/v1/Communities/teachers/{teacherUserId}`.

**States**:

- Loading/empty: preserve the existing teacher list behavior.
- Invite 409 `TeacherAlreadyBelongsToAnotherCommunity`: state that the identity already has a current staff relationship; do not offer automatic move/reassignment.
- Remove success: refresh the list and existing teacher-capacity display if present.
- 403: hide management actions after showing the permission error.

## Grades and Classes Section

**Visibility**: Active Community Owner or Teacher.

### Grade form/list

- Create: `POST /api/v1/Communities/grades` with `name`, `sortOrder`.
- List: `GET /api/v1/Communities/grades`.
- Existing table fields remain: name, sort order, class count, created date where currently shown.

### Class form/list

- Create: `POST /api/v1/Communities/classes` with `gradeId`, `name`.
- List: `GET /api/v1/Communities/classes?gradeId={gradeId}`; `gradeId` remains a resource filter, not tenant selection.
- Update: `PATCH /api/v1/Communities/classes/{classId}` with existing `name` and optional `gradeId` semantics.
- Delete: `DELETE /api/v1/Communities/classes/{classId}`.

**Table columns**:

- class name
- grade
- status/deletion state where currently shown
- created date where currently shown
- edit/delete actions

**States**:

- Preserve existing form validation and grade/class not-found behavior.
- Empty grade/class lists remain valid empty states.
- A foreign/stale gradeId or classId returns the existing error; do not retry with another community.

## Student License Management

**Visibility**: Community Owner only.

### List/filter

**Load**: `GET /api/v1/Communities/student-licenses`.

**Optional query fields retained**:

- `status`
- `gradeId`
- `classId`
- `search`

These identify records within the resolved community and are not tenant selectors.

**Table columns**:

- email
- status
- grade
- class
- email change count
- assigning user
- activation date
- created date
- edit/revoke actions

### Add/edit forms

**Fields**:

- `email`
- `gradeId`
- `classId`

**Actions**:

- Add: `POST /api/v1/Communities/student-licenses`.
- Edit: `PATCH /api/v1/Communities/student-licenses/{licenseId}`.
- Revoke: `DELETE /api/v1/Communities/student-licenses/{licenseId}`.

Preserve existing pending-only email-change, email-change-limit, capacity, duplicate-email, grade/class, confirmation, and post-success refresh behavior.

## Student Roster and Detail

**Visibility**: Active Community Owner or Teacher.

### Roster

**Load**: `GET /api/v1/Communities/students`.

**Optional query fields retained**:

- `status`
- `gradeId`
- `classId`
- `search`
- `pageNumber` (default `1`)
- `pageSize` (default `10`)

**Table columns**:

- email/name
- license status
- grade
- class
- activation date
- avatar where currently shown
- detail action when `playerProfileId` exists

Pending rows may retain null user/player/profile fields.

### Student detail

**Load**: `GET /api/v1/Communities/students/{playerProfileId}`.

Preserve existing profile, progression, placement, license, and placeholder-analytics display. A 404 means the profile is not available through the resolved community; do not try another CommunityId.

## Permission and Visibility Matrix

| Section/action | Owner | Teacher | Student | Platform Admin without staff membership |
|---|---:|---:|---:|---:|
| Load staff community `/me` | Yes | Yes | No | No |
| Edit community profile | Yes | No | No | No |
| Manage teachers | Yes | No | No | No |
| Manage grades/classes | Yes | Yes | No | No |
| Manage student licenses | Yes | No | No | No |
| View students | Yes | Yes | No | No |

UI visibility is advisory. Always handle backend 401/403 responses because membership and user status are mutable.

## Student Compatibility

Student community-profile access remains:

```http
GET /api/v1/Communities/{communityId}
```

Student clients continue supplying CommunityId for this retained endpoint. Do not migrate Student profile reads to `/Communities/me`.

## Platform Admin Compatibility

Platform Admin clients continue supplying an explicit CommunityId to:

- `POST /api/v1/admin/communities/{communityId}/owner`
- `PATCH /api/v1/admin/communities/{communityId}/licenses`

Do not apply the staff current-community interceptor or URL builder to Admin routes.

## Global Error Handling

- 401: end or repair the local session using existing behavior.
- 403: show unavailable/permission state; never ask the caller to choose or enter CommunityId.
- 404: treat the resource as absent from the current community.
- 409: show the operation-specific membership conflict; do not move staff automatically.
- Network/server failure: allow retry of the same community-ID-free request only.

## Out of Scope

- Frontend visual redesign.
- New community switcher behavior.
- Legacy membership conflict remediation UI.
- Platform Admin route changes.
- Student route redesign.
- Changes to token storage or JWT decoding beyond confirming no CommunityId is expected.
