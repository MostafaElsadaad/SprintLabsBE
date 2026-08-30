# Owner Community Profile Frontend Guide

## Feature Summary

Active community members can view basic community information. Active Owners can edit the community name and slug.

## Required Page or Section

Add a community profile section for the selected community:

- Read-only name, slug, and status for all active members.
- Edit action and form for active Owners only.

No dashboard, license, member, teacher, student, class, grade, or analytics UI belongs to this feature.

## Roles and Visibility

| Role/status | View | Edit |
|---|---:|---:|
| Active Owner | Yes | Yes |
| Active Teacher | Yes | No |
| Active Student | Yes | No |
| Pending or Removed membership | No | No |
| Platform admin without membership | No | No |

## Form Fields

| Field | Rules |
|---|---|
| Name | Required, trimmed, maximum 200 characters |
| Slug | Required, trimmed, lowercase, maximum 120 characters, unique |
| Status | Read-only |

PATCH sends both name and slug.

## Buttons and Actions

| Action | API |
|---|---|
| Load profile | `GET /api/v1/Communities/{communityId}` |
| Enter edit mode | Frontend-only; Owner visibility |
| Save changes | `PATCH /api/v1/Communities/{communityId}` |
| Cancel | Restore last loaded values |

## Loading, Empty, and Error States

- Show a profile loading state while GET is pending.
- Disable Save while PATCH is pending.
- A missing selected community is a navigation/context empty state.
- On 401, clear stale authentication.
- On 403, remove profile/edit access and refresh memberships.
- On 404, clear the stale selected community.
- On duplicate slug or invalid input, keep the form open and preserve values.
- Treat a 400 with `Record Already Exists` as a slug conflict.
- Treat a 403 as revoked/inactive membership or insufficient Owner role and refresh `/Users/me/communities`.

## API Flow

1. Load active memberships from `/api/v1/Users/me/communities`.
2. Select a returned `communityId`.
3. Load its profile with GET.
4. If the selected membership role is `Owner`, allow edit mode.
5. Submit both fields with PATCH and replace local profile state from the response.

## Open Questions

- None required for planning.

## Implementation Notes

- The API route is versioned as `/api/v1/Communities/{communityId}`.
- PATCH requires both name and slug even when only one value changed.
- Platform-admin status does not make the edit action visible without an Active Owner membership.

## Superseded Staff Contract

Use `GET`/`PATCH /api/v1/Communities/me` for staff profile flows and remove stored or supplied CommunityId. Keep the numeric GET only for Student-compatible profile access. See [feature 018 frontend handoff](../018-current-community-resolution/frontend.md).
