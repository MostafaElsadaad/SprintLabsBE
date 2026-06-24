# Admin Community Foundation Frontend Guide

## Feature Summary

This feature supports an internal platform-admin workspace for community setup. Admins can create communities, inspect setup summaries, assign owners, and configure license limits. It is not a community-owner dashboard.

## Required Frontend Page

### Platform Admin Communities

One admin-only page can cover the current scope:

- Community list/table.
- Create-community action and form.
- Assign-owner action for a selected community.
- Configure-license action for a selected community.

Separate pages are optional; the API does not require a particular navigation model.

## Roles and Access

| User | Visibility and access |
|---|---|
| Unauthenticated | No admin UI; redirect to sign-in |
| Authenticated non-admin | Hide admin navigation and handle API 403 |
| Suspended platform admin | No access; API returns 403 |
| Active platform admin | Full feature access |
| Community Owner/Teacher/Student | No admin access unless independently marked platform admin |

Use `GET /api/v1/Users/me` and `isPlatformAdmin` for initial visibility.

## Community Table

Recommended columns based only on returned data:

| Column | Source |
|---|---|
| Name | `name` |
| Slug | `slug` |
| Status | `status` |
| Owner | `owner.name`, `owner.email`, or unassigned state |
| Student licenses | `license.usedStudents` / `license.maxStudents` |
| Teacher licenses | `license.usedTeachers` / `license.maxTeachers` |
| Email change limit | `license.studentEmailChangeLimit` |
| Actions | Assign owner, configure licenses |

The list is already sorted by name by the backend.

## Forms and Fields

### Create Community

| Field | Type | Rules |
|---|---|---|
| Name | Text | Required; trim; backend DB maximum is 200 characters |
| Slug | Text | Required; trim; show lowercase preview; backend DB maximum is 120 characters |

### Assign Owner

| Field | Type | Rules |
|---|---|---|
| Email | Email | Required; trim; use normal email-format validation |
| Name | Text | Required; trim; user name DB maximum is 100 characters |

The backend may reuse an existing user and ignore the submitted name for that user.

### Configure Licenses

| Field | Type | Rules |
|---|---|---|
| Max students | Non-negative integer | Must be at least current `usedStudents` |
| Max teachers | Non-negative integer | Must be at least current `usedTeachers` |
| Student email change limit | Non-negative integer | Zero is allowed |

Show `usedStudents` and `usedTeachers` as read-only context, not editable inputs.

## Buttons and Actions

| Button/action | API call | After success |
|---|---|---|
| Create community | `POST /api/v1/admin/communities` | Close/reset form and add or reload row |
| Refresh | `GET /api/v1/admin/communities` | Replace table data |
| Assign owner | `POST /api/v1/admin/communities/{id}/owner` | Update/reload owner summary |
| Save license limits | `PATCH /api/v1/admin/communities/{id}/licenses` | Update/reload license summary |

There are no suspend, delete, invite, billing, teacher, or student actions in this feature.

## Validation Rules

- Trim all text before submit.
- Convert or preview slug in lowercase because the backend normalizes it.
- Do not promise that spaces or punctuation are rejected; the backend currently accepts them.
- Require whole numbers at or above zero for license inputs.
- Prevent maximum values below displayed used counts.
- Keep server-side duplicate-slug and permission errors authoritative.
- Do not allow editing used counts.

## Loading States

- Initial table loading state for `GET /admin/communities`.
- Per-form submit state with the relevant submit button disabled.
- Per-row saving state for owner/license actions so unrelated rows remain usable.
- Refresh stale list data after mutations.

## Empty States

- No communities: show a create-community action.
- `owner: null`: show "Owner not assigned" and the assign action.
- `license: null`: show "License limits not configured" and the configure action.
- Empty owner or license summaries are setup states, not page errors.

## Error States

| HTTP/state | UI behavior |
|---|---|
| 400 duplicate slug | Keep form open and mark slug as already used |
| 400 invalid license | Keep form open; verify non-negative values and usage floors |
| 400 owner input/user creation | Keep form open and show the returned message |
| 401 | Clear stale session and redirect to sign-in |
| 403 | Remove/hide admin workspace and show no-permission state |
| 404 community | Close stale row editor and reload the list |
| Network failure | Keep entered values and expose retry |

Authentication middleware 401 responses may have no JSON body, so status-code handling must not depend on `message`.

## Permissions and Visibility

- Hide the entire page and admin navigation unless `isPlatformAdmin` is true.
- Recheck authorization through API responses; do not trust only cached identity state.
- Community membership role does not grant access to this page.
- Do not expose controls for out-of-scope workflows.

## API Calls by User Flow

### Open Admin Communities

1. `GET /api/v1/Users/me`
2. If `isPlatformAdmin` is true, `GET /api/v1/admin/communities`

### Create Community

1. Submit create form to `POST /api/v1/admin/communities`.
2. On success, insert the returned community with null owner/license or reload the list.

### Assign Owner

1. Open form from a selected community row.
2. Submit to `POST /api/v1/admin/communities/{communityId}/owner`.
3. Replace the row's owner summary or reload.

### Configure Licenses

1. Prefill from the row's license summary, or zero values when null.
2. Submit to `PATCH /api/v1/admin/communities/{communityId}/licenses`.
3. Replace the row's license summary or reload.

## Implementation Notes / Spec Differences

- Mutations return 200 rather than 201.
- Duplicate slug is a 400 response.
- The backend only lowercases/trims slugs; it does not enforce URL-safe characters.
- Multiple active owners for different users are technically possible.
- Existing users' names are not changed during owner assignment.

## Open Questions

- Should the UI support replacing the current owner, or adding another owner?
- What slug format should product design require?
- Should owner assignment warn that an existing user's name will not be updated?
- Should suspended communities remain editable in this admin view when suspension management is added elsewhere?
