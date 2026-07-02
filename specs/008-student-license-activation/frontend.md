# Student License Activation on Login Frontend Guide

## Feature Summary

Students activate Pending student licenses by signing in with Google using the email assigned by a community Owner. The frontend does not add a separate invitation acceptance screen or activation action for this feature.

## Required Frontend Pages or Sections

### Existing Google Sign-In

- Continue using the existing Google sign-in entry point.
- Show the existing login loading and error states.
- Store the returned SprintLabs JWT, `userId`, and `playerProfileId` as before.

### Post-Login Community Refresh

- If the app shows community membership, refresh membership state after successful login.
- Newly activated Student memberships may appear after login for accounts with Pending licenses.

## User Roles That Can Access the Feature

| User type | Frontend access |
|---|---|
| Unauthenticated visitor | Can use Google sign-in |
| Student with Pending license | Uses Google sign-in; activation is automatic |
| Existing B2C player | Uses Google sign-in unchanged |
| Pending Teacher | Uses Google sign-in; existing teacher activation remains automatic |

Owners do not use this feature directly; they create Pending student licenses through the owner student-license management feature.

## Forms and Fields

No new SprintLabs form is required.

Google login still sends:

| Field | Source | Rules |
|---|---|---|
| `googleAccessToken` | Google SDK ID token | Required, non-empty, URL-encoded query parameter |

## Buttons and Actions

| Action | API call |
|---|---|
| Sign in with Google | `POST /api/v1/Account/google-login?googleAccessToken=...` |
| Refresh current user | `GET /api/v1/Users/me` |
| Refresh current communities | `GET /api/v1/Users/me/communities` |

No "accept student invitation" button is part of this feature.

## Tables and Columns

No new frontend table is required.

If an existing community list is shown after login, use the existing community-membership columns:

| Column | Source |
|---|---|
| Community name | Current-user communities response |
| Role | Current-user communities response, expected `Student` after activation |
| Membership status | Current-user communities response, expected `Active` after activation |

## Validation Rules

- Do not call login until the Google SDK returns a token.
- Do not try to match student licenses in frontend code; matching is server-owned.
- Do not infer community access from email. Refresh server state after login.
- Do not expect activation when the student's license is Revoked or already Active.

## Loading States

- Disable Google sign-in while login is pending.
- Keep the existing authenticated bootstrap/loading state while current user and communities refresh.
- Avoid showing stale "no community access" UI until post-login community refresh completes.

## Empty States

- If no communities are returned after login, show the existing no-community state.
- A successful login without new Student community access is valid for B2C players and users without Pending licenses.

## Error States

| State | Frontend behavior |
|---|---|
| Login 400/401 | Keep user signed out and allow Google sign-in retry |
| Login 403 | Clear session and show account unavailable/suspended state |
| Login 409 | Show a generic account-linking conflict message and retry guidance |
| Community refresh fails | Keep login session but show retry for membership loading |

## Permissions and Visibility Rules

- Student community access should come from refreshed membership APIs, not from the login email.
- Platform-admin status does not imply Student access.
- Teacher activation and Student activation can both happen during login; render roles from server responses.

## API Calls Used by Each User Action

| User action | API |
|---|---|
| Student signs in | `POST /api/v1/Account/google-login?googleAccessToken=...` |
| App bootstraps identity | `GET /api/v1/Users/me` |
| App loads activated communities | `GET /api/v1/Users/me/communities` |

## Implementation Notes / Spec Differences

- The feature brief refers to `/api/auth/google-login`; the implemented route is `/api/v1/Account/google-login`.
- Activation does not add response fields for activated licenses or communities.
- There is no manual invitation acceptance UI in this feature.

## Open Questions

- Should the frontend always refresh communities after login, or only when entering community-aware screens?
