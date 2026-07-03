# B2C Player Profile Frontend Notes

## Required Pages or Sections

- Current player profile view.
- Current player profile edit form for optional profile fields.

No community dashboard, assignments, reports, parent account, payment, export, import, or analytics UI is included in this feature.

## User Actions

- View own player profile after login.
- Update own optional profile fields: age, grade, and school name.
- Clear optional profile fields by submitting `null`.

## Forms and Fields

Profile edit form:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| age | number | No | Null or 1-120 |
| grade | number | No | Null or 1-20 |
| schoolName | text | No | Null or max 200 characters |

Do not expose editable user id, player profile id, community id, CommunityUser data, or StudentLicense data in this form.

## Validation Rules

- Age may be empty/null, otherwise 1-120.
- Grade may be empty/null, otherwise 1-20.
- School name may be empty/null, otherwise at most 200 characters.
- Client-side validation should mirror server rules, but server response remains the source of truth.

## Loading, Empty, and Error States

- Loading: show profile loading while `GET /api/v1/Users/me/player-profile` is in flight.
- Empty optional fields: render blank values for null age, grade, or school name.
- Save in progress: disable duplicate submits while `PATCH /api/v1/PlayerProfiles/me` is in flight.
- 400: show validation feedback for invalid profile values.
- 401: send the user through the existing auth flow.
- 403: show the existing forbidden/suspended-user handling.
- 404: show existing profile-not-found handling.

## Permissions and Visibility

- Any authenticated B2C or B2B user with a PlayerProfile can use these profile endpoints.
- Community Owner, Teacher, Student, Platform Admin, CommunityUser, and StudentLicense roles are not required for B2C profile view or edit.
- Do not hide B2C profile editing just because the user has zero communities.

## API Calls Used

- On profile screen load: `GET /api/v1/Users/me/player-profile`.
- On profile save: `PATCH /api/v1/player-profiles/me`.

## Open Questions

- None for the scoped B2C profile view/update workflow.
