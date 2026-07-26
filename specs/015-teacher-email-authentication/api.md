# Teacher Email Authentication API

## Feature Summary

This planned API adds email/password authentication for teacher-marked accounts and explicit acceptance of community invitations. Teacher identity is independent of membership: registration, confirmation, login, refresh, logout, and recovery all work with zero communities. Community access still requires an Active `CommunityUser`.

The canonical request/response examples and error statuses are in [contracts/teacher-authentication-api.md](./contracts/teacher-authentication-api.md).

## Route Summary

| Method | Route | Auth / role | Frontend purpose |
|---|---|---|---|
| POST | `/api/v1/Account/teachers/register` | Public | Create or complete an eligible teacher account |
| POST | `/api/v1/Account/confirm-email` | Public | Confirm teacher email from a link |
| POST | `/api/v1/Account/resend-confirmation` | Public | Request another confirmation email |
| POST | `/api/v1/Account/teachers/login` | Public | Sign in a confirmed teacher |
| POST | `/api/v1/Account/refresh-token` | Raw refresh token | Rotate session credentials |
| POST | `/api/v1/Account/logout` | Raw refresh token | Revoke one session |
| POST | `/api/v1/Account/forgot-password` | Public | Request recovery without enumeration |
| POST | `/api/v1/Account/reset-password` | Public | Set a new password from a link |
| POST | `/api/v1/Communities/{communityId}/teachers/invite` | Bearer + Active Owner | Invite/reissue a Teacher membership |
| POST | `/api/v1/CommunityInvitations/accept` | Bearer + confirmed Teacher | Explicitly activate one pending membership |
| GET | `/api/v1/Users/me/communities` | Bearer | Load all active memberships |

## Authentication and Visibility Rules

- Registration and password login are teacher-only.
- `IsTeacherAccount` identifies the account type but grants no community permission.
- A teacher can log in with `communities: []`.
- Only Active memberships appear in login and `/Users/me/communities`.
- Owners continue to be authorized through Active Owner membership in the route-selected community.
- Invitation acceptance uses the bearer token's `userId`; the request body cannot select another user.
- The normalized authenticated email must match the invitation email.
- Community roles are not embedded in access tokens.

## Request Validation

### Registration and Reset Password

- Name: required for registration, trimmed, maximum 100 characters.
- Email: required, valid, trimmed and normalized.
- Password: minimum 8 characters with uppercase, lowercase, digit, and non-alphanumeric character.
- Existing eligible passwordless invitation identities are reused.
- Existing non-teacher Google/player identities produce a safe conflict rather than account linking.

### Confirmation and Reset Links

- Identity token is Base64URL encoded.
- Confirmation includes `userId` and token.
- Reset includes email and token.
- Malformed, expired, or mismatched values never change account state.

### Refresh and Invitation Tokens

- Tokens are opaque, case-sensitive bearer values.
- Frontend must submit them exactly as received.
- Refresh tokens are single-use and rotate on every successful refresh.
- Invitation tokens expire, can be superseded, and can be accepted only once.

## Success and Error Envelopes

All planned controller responses use existing `BaseResponse` JSON. `statusCode` and `errorCode` remain numeric. Authentication middleware may still return an empty `401` for a missing/invalid bearer token before the controller runs.

Frontend must:

- Use the HTTP status as authoritative.
- Never log passwords, access tokens, refresh tokens, confirmation tokens, reset tokens, or invitation tokens.
- Replace both stored access and refresh tokens atomically after refresh.
- Clear the local session when refresh fails or after logout.
- Treat the forgot-password response identically for every submitted email.

## Email Links

Configured client links:

```text
{FrontendBaseUrl}/confirm-email?userId={userId}&token={urlSafeToken}
{FrontendBaseUrl}/reset-password?email={encodedEmail}&token={urlSafeToken}
{FrontendBaseUrl}/invitations/accept?token={rawInvitationToken}
```

The backend does not implement these frontend pages.

## Community Invitation Notes

- The existing Owner invite endpoint is updated; no duplicate Owner invitation API is added.
- New and restored memberships are Pending and reserve one seat.
- Resend/reissue revokes the previous usable invitation but does not reserve another seat.
- Acceptance changes Pending to Active without changing `UsedTeachers`.
- Removal revokes outstanding invitations and releases a counted seat once.
- An Active membership is an idempotent invite/accept result.
- A teacher may accept invitations for multiple communities independently.

## Loading, Empty, and Recovery Guidance

- Registration/forgot/resend return `202` because delivery is asynchronous from the user's perspective.
- Disable repeated form submission until the response is received.
- Respect `resendAvailableAt` on confirmation UI.
- Login with `communities: []` is successful; display "You're not joined in a community yet."
- A `401` from refresh ends the local session.
- A `410` invitation response should show that the link expired or was replaced and direct the teacher to request a new invitation from the community Owner.
- A `503` email error means the account or pending invitation may already exist; do not automatically create a second identity or membership.

## Compatibility

- Existing `POST /api/v1/Account/google-login` request and `LoginResponse` remain unchanged.
- Google player login continues creating/reusing Player profiles and activating eligible student licenses.
- Google login no longer activates pending Teacher memberships.
- Existing Active Teacher memberships remain active.
- Existing `/Users/me/communities`, Owner authorization, and membership authorization retain their contracts.

## Open Questions

- Production SMTP provider/library approval is deferred; the planned implementation uses configured SMTP suitable for Mailpit/Mailtrap and basic authenticated SMTP.

