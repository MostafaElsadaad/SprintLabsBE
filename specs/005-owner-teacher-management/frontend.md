# Owner Teacher Management Frontend Guide

## Feature Summary

Active community Owners can manage teacher memberships for a selected community: invite teachers, view teacher status, and remove teachers without deleting membership history.

## Required Page or Section

Add a Teacher Management section inside the owner-facing community area:

- Teacher invite form.
- Teacher list table.
- Remove action for Teacher rows.
- Seat-capacity error handling.

Do not add email sending, teacher dashboard, student management, grades, classes, analytics, payments, or admin teacher-management UI for this feature.

## Roles and Visibility

| Role/status | View teacher management | Invite | Remove |
|---|---:|---:|---:|
| Active Owner | Yes | Yes | Yes |
| Active Teacher | No | No | No |
| Active Student | No | No | No |
| Pending or Removed Owner | No | No | No |
| Platform admin without Active Owner membership | No | No | No |

Use `/api/v1/Users/me/communities` to determine visible communities and role labels, but rely on the server for final authorization.

## Invite Form

Fields:

| Field | Rules |
|---|---|
| Email | Required, valid email, trimmed before submit |
| Name | Required, trimmed |

Buttons/actions:

| Action | API |
|---|---|
| Invite teacher | `POST /api/v1/Communities/{communityId}/teachers/invite` |
| Reset form | Frontend-only |

After success:

- Clear the form.
- Refresh the teacher list.
- Update any local teacher-seat display if shown.

## Teacher Table

Columns:

| Column | Source |
|---|---|
| Name | `name` |
| Email | `email` |
| Status | `status` |
| Invited/Created date | `createdAt` |
| Actions | Show remove for Teacher rows that are not already Removed |

Statuses:

- `Pending`: invited, seat reserved, no active community access yet.
- `Active`: signed in and activated, has active Teacher access.
- `Removed`: no active community access.

Inviting an email that already belongs to a logged-in SprintLabs user may return `Active` immediately. Inviting a new placeholder user returns `Pending`.

## Remove Action

API:

```http
DELETE /api/v1/Communities/{communityId}/teachers/{userId}
```

Behavior:

- Confirm before removing.
- Disable the row action while the request is pending.
- On success, update the row to `Removed` or refresh the list.
- Do not expose this action for Owner memberships.

## Loading, Empty, and Error States

- Show a loading state while fetching teachers.
- Empty state: "No teachers invited yet" for an empty list.
- Disable invite submit while invitation is pending.
- On 400 full-capacity response, show that no teacher seats are available.
- On 400 invalid input, keep form values and show field-level feedback where possible.
- On 401, clear stale authentication.
- On 403, hide teacher management and refresh memberships.
- On 404, refresh selected community and teacher list context.

## API Calls by User Action

| User action | API |
|---|---|
| Load owner memberships | `GET /api/v1/Users/me/communities` |
| Load teachers | `GET /api/v1/Communities/{communityId}/teachers` |
| Invite teacher | `POST /api/v1/Communities/{communityId}/teachers/invite` |
| Remove teacher | `DELETE /api/v1/Communities/{communityId}/teachers/{userId}` |
| Teacher accepts by login | Existing Google login flow; no separate frontend acceptance endpoint |

## Permissions and Seat Visibility

If the UI shows teacher seats, count Pending plus Active teachers as used. Removed teachers do not count as used seats.

Duplicate invites for an existing Pending or Active Teacher are idempotent from the frontend perspective: show the returned teacher row and refresh the list. If the user has logged in since becoming Pending, a repeated invite may upgrade the row to `Active` without consuming another seat.

## Open Questions

- None for this implementation.

## Implementation Notes

- Pending teachers activate on matching Google login; the frontend does not call a separate activation endpoint.
- Removed teachers remain visible in the list with `status = "Removed"` so Owners can see the removal state.
