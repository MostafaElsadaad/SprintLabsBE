# Teacher Email Authentication Frontend Requirements

## Scope

Frontend implementation is outside this backend feature. This document defines the pages, fields, actions, states, and visibility the API supports; it does not prescribe a frontend framework or visual design.

## Pages and Sections

| Page / section | Audience | API actions |
|---|---|---|
| Teacher registration | Public | Register teacher |
| Confirm email | Link recipient | Confirm email; resend confirmation |
| Teacher login | Public | Password login |
| Forgot password | Public | Request reset |
| Reset password | Link recipient | Reset password |
| No-community state | Authenticated teacher | Uses login/current communities |
| Invitation acceptance | Authenticated teacher from link | Accept invitation |
| Owner teacher management | Active community Owner | Existing list/invite/remove operations |

## Teacher Registration

Fields:

| Field | Rules |
|---|---|
| Name | Required; trim; maximum 100 characters |
| Email | Required; valid email; trim |
| Password | Required; at least 8 characters; uppercase, lowercase, digit, special character |

Actions and states:

- Submit to `POST /api/v1/Account/teachers/register`.
- Disable submit and show loading while pending.
- On `202`, show the response message and confirmation instructions.
- Store/display `resendAvailableAt`; do not treat registration as login.
- On `409`, explain that the existing account cannot currently be linked to teacher password authentication.
- On `503`, show delivery failure and allow resend after cooldown; do not retry registration by creating another account.

## Confirm Email

Inputs come from the link query: `userId` and `token`.

- Call `POST /api/v1/Account/confirm-email` once on page load or explicit confirmation action.
- Show confirming, success, invalid/expired, and general error states.
- Repeated success is valid.
- Confirmation does not join any community.
- Provide a resend form/action using the teacher's email when the link is invalid or expired.
- Disable resend until `resendAvailableAt`.

## Teacher Login

Fields: email and password.

- Call `POST /api/v1/Account/teachers/login`.
- Store access token, access expiry, refresh token, and refresh expiry using the frontend's secure session strategy.
- Replace both tokens after every successful refresh.
- Never expose token values in URLs, analytics, or application logs.
- On `401`, show generic invalid credentials.
- On `403`, show the applicable confirmation/suspension state without revealing other account information.
- On `423`, show temporary lockout guidance.
- If `communities` is empty, show: "You're not joined in a community yet."
- If communities exist, populate the existing community selection/navigation using active rows only.

## Session Refresh and Logout

- Refresh shortly before access expiry or once after an authenticated request returns `401`.
- Call `POST /api/v1/Account/refresh-token` with the current refresh token.
- Prevent parallel refresh requests from reusing one token; coordinate callers around one in-flight refresh.
- On success, atomically replace access and refresh tokens and expiries.
- On failure, clear the local session and return to login.
- Logout calls `POST /api/v1/Account/logout`, then clears the local session even if the request is repeated.

## Forgot and Reset Password

Forgot-password field: email.

- Always show "If an account exists, a password reset email has been sent."
- Do not change wording based on timing or account assumptions.

Reset fields:

| Field | Source / rule |
|---|---|
| Email | Link query; display read-only or hidden |
| Token | Link query; never display or log |
| New password | Same policy as registration |
| Confirm password | Frontend validation; must match new password |

- On success, clear existing local tokens and navigate to teacher login.
- Invalid/expired token shows a safe failure and offers the forgot-password flow.

## Invitation Acceptance

The link provides only the invitation token.

1. If not authenticated, preserve the token in secure in-memory/session navigation state and send the user to teacher login or registration.
2. If authenticated, call `POST /api/v1/CommunityInvitations/accept`.
3. On success, add/refresh the returned Active community membership.
4. Repeated success is valid and displays the same community.
5. On `403`, explain that the signed-in confirmed teacher account must match the invited email; offer logout/switch account.
6. On `410`, show expired/replaced guidance and tell the user to contact the community Owner.
7. Do not expose or copy the token into analytics, logs, or error reporting.

## Owner Teacher Management Changes

- Existing list/invite/remove pages remain Owner-only.
- Invite results are always Pending for newly issued password-teacher invitations, even when the email belongs to a registered teacher.
- Rename any "activates on login" copy to "awaiting explicit acceptance."
- A resend uses the existing invite action for a Pending membership; it does not add a seat.
- After invite/resend, refresh the teacher list.
- Existing Active membership is an idempotent result.
- Removal marks the row Removed and invalidates outstanding links.

## Loading, Empty, and Error States

| Operation | Loading | Empty / success | Error |
|---|---|---|---|
| Register | Disable form | Check-email message | Field validation, conflict, email unavailable |
| Confirm | Confirming indicator | Confirmed; login action | Invalid/expired; resend option |
| Resend | Disable until cooldown | Generic check-email message | Email unavailable |
| Login | Disable form | Community selector or no-community message | Invalid, unconfirmed, suspended, locked |
| Forgot | Disable form | Always generic message | Only malformed request/general availability |
| Reset | Disable form | Password changed; login action | Policy or invalid/expired token |
| Accept invite | Joining indicator | Community joined | Switch account, expired/replaced, invalid |
| Refresh | Background single-flight | Session continues | Clear session |

## Permissions and Visibility

- Teacher authentication pages are public except invitation acceptance itself.
- Community management remains visible only for Active Owner memberships.
- Teacher account marking is not a frontend authorization signal.
- Community feature visibility may use the active membership role returned by the backend, but every API call must still handle backend authorization.
- Platform admins and players do not receive password-login UI through this feature unless they separately possess an eligible teacher account.

## API Calls by Action

| User action | API |
|---|---|
| Register | `POST /api/v1/Account/teachers/register` |
| Confirm email | `POST /api/v1/Account/confirm-email` |
| Resend confirmation | `POST /api/v1/Account/resend-confirmation` |
| Teacher login | `POST /api/v1/Account/teachers/login` |
| Refresh session | `POST /api/v1/Account/refresh-token` |
| Logout | `POST /api/v1/Account/logout` |
| Forgot password | `POST /api/v1/Account/forgot-password` |
| Reset password | `POST /api/v1/Account/reset-password` |
| Accept invite | `POST /api/v1/CommunityInvitations/accept` |
| Owner invite/resend | `POST /api/v1/Communities/{communityId}/teachers/invite` |
| Load active communities | `GET /api/v1/Users/me/communities` |

## Open Questions

- The frontend's secure token-storage mechanism depends on the client platform and is outside this backend specification.

