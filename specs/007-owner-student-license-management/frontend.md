# Owner Student License Management Frontend Guide

## Feature Summary

Active community Owners can manage student seats by adding licenses, reviewing and filtering licenses, correcting Pending license emails, changing grade/class assignment, and revoking licenses. Added licenses are Pending for not-yet-registered students and Active immediately when the email already belongs to a Google-registered user.

## Required Page or Section

Add a Student Licenses section inside the owner-facing community area:

- Add student license form.
- Student license table with filters.
- Edit action for Pending license email and grade/class assignment.
- Revoke action for non-revoked licenses.
- Capacity feedback using community license student limits when available from admin/community data.

Do not add student activation, email sending, bulk import, dashboards, analytics, payments, parent accounts, teacher management, or grade/class management UI for this feature.

## Roles and Visibility

| Role/status | View | Add | Update | Revoke |
|---|---:|---:|---:|---:|
| Active Owner | Yes | Yes | Yes | Yes |
| Active Teacher | No | No | No | No |
| Active Student | No | No | No | No |
| Pending or Removed Owner | No | No | No | No |
| Platform admin without Active Owner membership | No | No | No | No |

Use `/api/v1/Users/me/communities` for visibility, but rely on server 403 as final authority.

## Forms and Fields

### Add License

| Field | Rules |
|---|---|
| Email | Required, valid email, max 256 characters |
| Grade | Required, same selected community |
| Class | Required, active class under selected grade |

API: `POST /api/v1/Communities/{communityId}/student-licenses`

### Update License

| Field | Rules |
|---|---|
| Email | Required, only editable while status is Pending |
| Grade | Required |
| Class | Required, active class under selected grade |

API: `PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}`

## Table Columns

| Column | Source |
|---|---|
| Email | `email` |
| Status | `status` |
| Grade | `grade.name` |
| Class | `class.name` |
| Email changes | `emailChangeCount` |
| Assigned by | `assignedByName` or `assignedByUserId` |
| Activated date | `activatedAt` |
| Created date | `createdAt` |
| Actions | Edit for Pending, revoke for Pending/Active |

## Filters

| Filter | API query |
|---|---|
| Status | `status` |
| Grade | `gradeId` |
| Class | `classId` |
| Search | `search` |

## Buttons and Actions

| Action | API |
|---|---|
| Load licenses | `GET /api/v1/Communities/{communityId}/student-licenses` |
| Add license | `POST /api/v1/Communities/{communityId}/student-licenses` |
| Update license | `PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}` |
| Revoke license | `DELETE /api/v1/Communities/{communityId}/student-licenses/{licenseId}` |

## Loading, Empty, and Error States

- Show a loading state while fetching licenses.
- Empty state: no student licenses for current filters.
- Disable form buttons while requests are pending.
- On 400 capacity errors, show that no student seats are available.
- On 400 duplicate email, show that the student already has a non-revoked license.
- On 400 email limit errors, show that the email can no longer be changed.
- On 401, clear stale authentication.
- On 403, hide the section and refresh memberships.
- On 404, refresh grade/class/license data because the selected record may be stale.

## Permissions and Visibility Rules

- Show this section only for Active Owner memberships.
- Hide update email controls unless status is Pending.
- Hide or disable revoke for already Revoked licenses.
- Re-fetch licenses after add, update, or revoke.
- After add, render the returned `status` instead of assuming the new row is Pending.

## API Calls by User Action

| User action | API |
|---|---|
| Open student license section | `GET /api/v1/Communities/{communityId}/student-licenses` |
| Change filters | `GET /api/v1/Communities/{communityId}/student-licenses?status={status}&gradeId={gradeId}&classId={classId}&search={search}` |
| Add student license | `POST /api/v1/Communities/{communityId}/student-licenses` |
| Save edits | `PATCH /api/v1/Communities/{communityId}/student-licenses/{licenseId}` |
| Revoke license | `DELETE /api/v1/Communities/{communityId}/student-licenses/{licenseId}` |

## Open Questions

- None for this implementation.

## Implementation Notes

- Adding an already Google-registered student activates the license immediately and creates Student community access.
- Not-yet-registered students remain Pending until login activation.
- Revoked licenses remain visible in list results for audit/history unless filtered out by the frontend.

## Superseded Staff Contract

Use the community-ID-free student-license calls from [feature 018 frontend handoff](../018-current-community-resolution/frontend.md); retained filters identify records only within the server-resolved community.
