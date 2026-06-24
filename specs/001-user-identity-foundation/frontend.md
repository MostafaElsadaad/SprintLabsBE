# User Identity Foundation Frontend Guide

## Feature Summary

The frontend uses this feature to establish a Sprint Labs session from Google, bootstrap the signed-in user, and load the optional game player profile. `User` is the shared identity; `playerProfileId` and the player-profile endpoint represent game-specific progress.

## Required Frontend Sections

### Google Sign-In Entry

- Google sign-in button or the existing game login action.
- Login progress indicator while exchanging the Google ID token.
- Controlled login failure message.

### Authenticated User Bootstrap

- A session/bootstrap layer that calls `GET /api/v1/Users/me`.
- Shared state for `userId`, identity fields, `isPlatformAdmin`, and `playerProfileId`.
- Navigation visibility derived from the returned identity.

### Player Profile Section

- Existing player/profile screen showing avatar, name, school/profile fields, and progression.
- This section is available only when `playerProfileId` is present or the profile request succeeds.

## User Roles

| User type | Access |
|---|---|
| Unauthenticated visitor | Google login only |
| Active authenticated user | Current identity |
| Active user with player profile | Current identity and player profile |
| Platform admin | Same identity access; admin navigation may be shown from `isPlatformAdmin` |
| Suspended user | Login/current-user calls are rejected |

## Forms and Fields

Google login has no Sprint Labs form fields. The frontend obtains a Google ID token from the Google SDK and sends it as `googleAccessToken`.

The player-profile response is read-only in this feature:

| Field | UI use |
|---|---|
| `pictureUrl` | Avatar; allow a fallback image when null |
| `name` | Display name |
| `email` | Account identity |
| `gold` | Progression balance |
| `experience` | Progression value |
| `level` | Current level |
| `schoolName` | Optional profile detail |
| `age` | Optional profile detail |
| `grade` | Optional profile detail |

Profile editing is not part of this Spec Kit feature.

## Buttons and Actions

| Action | API call |
|---|---|
| Sign in with Google | `POST /api/v1/Account/google-login?googleAccessToken=...` |
| Restore/refresh signed-in identity | `GET /api/v1/Users/me` |
| Open game profile | `GET /api/v1/Users/me/player-profile` |
| Sign out | Frontend clears the stored Sprint Labs JWT; no logout endpoint exists in this feature |

## Tables

No table is required for this feature.

## Validation Rules

- Do not call login until the Google SDK returns a non-empty ID token.
- Treat a missing `accessToken` in a nominally successful response as a failed login.
- Treat `playerProfileId` as nullable in shared identity state.
- Do not infer platform-admin access from email or Google domain; use `isPlatformAdmin`.
- Do not accept arbitrary user IDs for current-user screens; these endpoints are JWT-scoped.

## Loading States

- Disable the Google sign-in action while login is in progress.
- Show an application bootstrap/loading state while `/Users/me` is pending.
- Show a profile skeleton or local loading indicator while the player profile is loading.
- Avoid briefly rendering admin or game-only navigation before identity is resolved.

## Empty States

- `playerProfileId: null`: show a neutral "No player profile" state or hide game-profile navigation.
- Null avatar: use the product's default avatar treatment.
- Null school, age, or grade: omit the field or show the existing neutral placeholder.

## Error States

| State | Frontend behavior |
|---|---|
| Login 400/401 | Keep user signed out and offer Google sign-in retry |
| Login/current-user 403 | Clear the session and show an account unavailable/suspended message |
| `/Users/me` 401 | Clear the local JWT and return to sign-in |
| `/Users/me` 404 | Clear stale session state; the token points to a missing user |
| Player profile 404 | Keep the identity session active; show no-player-profile state |
| Network/server error | Preserve safe local state and expose a retry action |

Do not rely only on `message`; use HTTP status first, then `errorCode` and `message` for detail.

## Permissions and Visibility

- Show platform-admin navigation only when `/Users/me` returns `isPlatformAdmin: true`.
- Show game-profile navigation only when `playerProfileId` is non-null.
- Never use `playerProfileId` as the authenticated identity key.
- A platform admin does not automatically have a player profile.

## Suggested Call Flow

1. Google SDK returns an ID token.
2. Frontend calls Google login.
3. Frontend stores the Sprint Labs JWT and immediate login data.
4. Frontend calls `/api/v1/Users/me` to establish canonical session identity.
5. If `playerProfileId` is present and the current screen needs profile data, call `/me/player-profile`.

## Implementation Notes / Spec Differences

- Login currently sends the Google token in the query string, not a request body.
- Login always follows the game/player flow and creates or finds a player profile.
- Missing Google display name falls back to email.
- Authentication-generated 401 responses may not contain the normal JSON envelope.

## Open Questions

- What secure client storage strategy is required for the seven-day JWT on each frontend platform?
- Should users without a player profile receive a profile-creation action, or should the game login remain the only creation path?
- Is there a product-approved suspended-account message distinct from the backend's `InvalidAccessToken` text?
