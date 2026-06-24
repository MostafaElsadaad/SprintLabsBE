# Community Access Foundation Frontend Guide

## Feature Summary

This feature gives the frontend the signed-in user's active community memberships. It supports community selection and role-aware navigation, but it does not implement teacher, student, owner-dashboard, class, grade, analytics, license-consumption, or payment screens.

## Required Frontend Sections

### Community Switcher or Community List

Display the active memberships returned by:

```http
GET /api/v1/Users/me/communities
```

The frontend may use a menu, selector, or list depending on the existing product shell. Each option needs enough information to identify the community, status, and current user's role.

### Community Context State

Store at least:

- `communityId`
- `communityName`
- `slug`
- `communityStatus`
- `role`
- `membershipStatus`

Do not persist a selected community that is no longer present in a refreshed response.

## User Roles

| Membership role | Returned when | Frontend implication |
|---|---|---|
| Owner | Membership is Active | May show future owner navigation only when those features exist |
| Teacher | Membership is Active | May show future teacher navigation only when those features exist |
| Student | Membership is Active | May show future student navigation only when those features exist |
| Pending | Never returned | No access UI from this endpoint |
| Removed | Never returned | No access UI |
| Platform admin without membership | Not returned | Admin status does not create community access |

## Forms and Fields

No create/edit form is part of this feature.

If the application uses a community selector, its option label can use:

- Primary: `communityName`
- Secondary: `role`
- Status indicator: `communityStatus`

## Buttons and Actions

| Action | API call |
|---|---|
| Load/refresh my communities | `GET /api/v1/Users/me/communities` |
| Select community | No API call in this feature; update frontend context |
| Retry failed load | Repeat `GET /api/v1/Users/me/communities` |

Do not add invite acceptance, membership removal, role editing, or community management actions from this feature.

## Tables and Columns

A table is optional. If a full list/table is used, supported columns are:

| Column | Field |
|---|---|
| Community | `communityName` |
| Slug | `slug` |
| Community status | `communityStatus` |
| My role | `role` |
| Membership status | `membershipStatus` |

`membershipStatus` is currently always `Active` in successful list items.

## Validation Rules

- Only select a community returned for the current session.
- Treat `communityId` as the stable context key.
- Accept role values `Owner`, `Teacher`, and `Student`.
- Accept community status values `Active` and `Suspended`.
- Do not derive access from slug, platform-admin status, or previously cached memberships.
- Revalidate the selected community after session changes or list refresh.

## Loading States

- Show a compact loading state while memberships are requested.
- Delay community-dependent navigation until the response is resolved.
- When switching accounts, clear the prior user's communities immediately.
- A background refresh should not silently switch the user's selected community unless it is no longer valid.

## Empty States

When `data` is an empty array:

- Show a neutral "No active communities" state.
- Keep non-community/player experiences available when appropriate.
- Do not show Pending or Removed memberships as unavailable rows because the API intentionally excludes them.

## Error States

| HTTP/state | Frontend behavior |
|---|---|
| 401 | Clear stale auth state and return to sign-in |
| 403 | Clear community context and show account unavailable/suspended state |
| 404 | Clear session/context because the JWT user no longer exists |
| Network failure | Keep community-dependent content closed and expose retry |

Authentication middleware 401 responses may have no response body.

## Permissions and Visibility

- A returned Active membership grants community visibility in the selector.
- Hide a community when it disappears from a refreshed list.
- Mark `communityStatus: "Suspended"` clearly and avoid navigating into it until product rules are defined.
- Role-based frontend visibility is advisory; future APIs must still enforce roles on the backend.
- Platform-admin UI remains controlled by `isPlatformAdmin` from `/Users/me`, not by this community list.

## API Calls by User Flow

### Application Bootstrap

1. Call `GET /api/v1/Users/me`.
2. Call `GET /api/v1/Users/me/communities`.
3. Restore the last selected `communityId` only if it remains in the returned list.
4. Otherwise select according to existing product behavior or ask the user to choose.

### Refresh Community Context

1. Repeat `/Users/me/communities`.
2. Replace cached memberships.
3. Clear or change the current selection if its membership is no longer returned.

## Implementation Notes / Spec Differences

- Use `communityName` in actual JSON. The generated OpenAPI planning contract says `name`, but the DTO and handler return `communityName`.
- Suspended communities are included when membership is Active.
- No role-specific authorization attribute/filter is exposed to the frontend or added to endpoints in this feature.
- There is no API for selecting a community; selection is frontend state until a future endpoint requires `communityId`.

## Open Questions

- Should suspended communities be disabled, hidden, or selectable in the community switcher?
- What is the default selection rule when a user belongs to multiple communities?
- Should the selected community be persisted locally, in the URL, or in server-side user preferences?
- When invitations are implemented later, will Pending memberships appear in a separate endpoint or section?
