# Community Grades and Classes Frontend Guide

## Feature Summary

Active community Owners and Teachers can create grades, view grades with active class counts, create/list/update classes, and soft delete classes. Students and inactive memberships cannot manage this structure.

## Required Page or Section

Add a Grades and Classes management section inside the selected community area:

- Grade create form.
- Grade list with class counts.
- Class create form.
- Class list with optional grade filter.
- Class edit action for name and grade movement.
- Class delete action that removes the class from active views after success.

Do not add student assignment, student management, licenses, analytics, imports, payment, dashboard, or grade edit/delete UI for this feature.

## Roles and Visibility

| Role/status | Manage grades/classes |
|---|---:|
| Active Owner | Yes |
| Active Teacher | Yes |
| Active Student | No |
| Pending or Removed membership | No |
| Platform admin without Active Owner/Teacher membership | No |

Use `/api/v1/Users/me/communities` to determine visible communities and role labels, but rely on server-side 403 as the final authority.

## Forms and Fields

### Create Grade

| Field | Rules |
|---|---|
| Name | Required, trimmed, maximum 120 characters |
| Sort order | Required number, zero or greater |

API: `POST /api/v1/Communities/{communityId}/grades`

### Create Class

| Field | Rules |
|---|---|
| Grade | Required, must be one of the loaded grades for the selected community |
| Name | Required, trimmed, maximum 120 characters |

API: `POST /api/v1/Communities/{communityId}/classes`

### Update Class

| Field | Rules |
|---|---|
| Name | Required, trimmed, maximum 120 characters |
| Grade | Optional from API perspective; if shown, must be a loaded same-community grade |

API: `PATCH /api/v1/Communities/{communityId}/classes/{classId}`

## Buttons and Actions

| Action | API |
|---|---|
| Load grades | `GET /api/v1/Communities/{communityId}/grades` |
| Create grade | `POST /api/v1/Communities/{communityId}/grades` |
| Load classes | `GET /api/v1/Communities/{communityId}/classes` |
| Filter classes by grade | `GET /api/v1/Communities/{communityId}/classes?gradeId={gradeId}` |
| Create class | `POST /api/v1/Communities/{communityId}/classes` |
| Update class | `PATCH /api/v1/Communities/{communityId}/classes/{classId}` |
| Delete class | `DELETE /api/v1/Communities/{communityId}/classes/{classId}` |

## Tables and Columns

### Grade Table

| Column | Source |
|---|---|
| Grade name | `name` |
| Sort order | `sortOrder` |
| Class count | `classCount` |
| Created date | `createdAt` |

### Class Table

| Column | Source |
|---|---|
| Class name | `name` |
| Grade | Resolve `gradeId` against loaded grade list |
| Status | `status` |
| Created date | `createdAt` |
| Actions | Edit and delete for active rows |

The class list endpoint already excludes deleted classes, so active class tables normally should not show `Deleted` rows.

## Loading, Empty, and Error States

- Show loading states while fetching grades/classes.
- Disable submit buttons while create/update/delete requests are pending.
- Empty grades: prompt the Owner/Teacher to create the first grade.
- Empty classes: show no classes for the selected filter.
- On 400 invalid input, keep form values and show field-level feedback when possible.
- On 401, clear stale authentication.
- On 403, hide the management section and refresh memberships.
- On 404 for grade/class operations, refresh grades/classes because the selected row or grade may be stale.
- After class create, move, or delete, refresh grades so `classCount` stays accurate.

## Permissions and Visibility Rules

- Show this section only for Active Owner or Active Teacher memberships.
- Do not show for Students, Pending memberships, Removed memberships, or platform-admin-only users.
- The frontend may use membership role/status from `/api/v1/Users/me/communities`, but must still handle 403 from every endpoint.

## API Calls by User Action

| User action | API |
|---|---|
| Open community structure page | `GET /api/v1/Communities/{communityId}/grades` and `GET /api/v1/Communities/{communityId}/classes` |
| Create grade | `POST /api/v1/Communities/{communityId}/grades` |
| Create class | `POST /api/v1/Communities/{communityId}/classes` |
| Filter class table | `GET /api/v1/Communities/{communityId}/classes?gradeId={gradeId}` |
| Rename or move class | `PATCH /api/v1/Communities/{communityId}/classes/{classId}` |
| Remove class | `DELETE /api/v1/Communities/{communityId}/classes/{classId}` |

## Open Questions

- None for this implementation.

## Implementation Notes

- The API route is versioned as `/api/v1/Communities/...`.
- Class deletion is soft delete; no undo endpoint is implemented.
- Grade update and delete are intentionally not implemented.

## Superseded Staff Contract

Use the community-ID-free grade and class calls from [feature 018 frontend handoff](../018-current-community-resolution/frontend.md); `gradeId` remains a resource filter, not a tenant selector.
