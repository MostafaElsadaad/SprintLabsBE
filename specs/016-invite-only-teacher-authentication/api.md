# API Guide: Invite-Only Teacher Authentication

This feature changes the existing teacher password lifecycle; it does not add a parallel authentication stack. All endpoints use the current API versioning and `BaseResponse<T>` conventions. The exact payloads are defined in [contracts/invite-only-teacher-authentication-api.md](./contracts/invite-only-teacher-authentication-api.md).

## Lifecycle

1. An authenticated Active Owner invites an email through the existing community route.
2. The backend reserves one Pending Teacher membership and emails a one-time setup link.
3. The anonymous setup page validates the token and displays community name plus masked email.
4. The teacher submits name and password; the backend confirms the invitation email and activates the existing membership.
5. The frontend redirects to teacher login. Completion does not return tokens.
6. Password login returns the teacher's one active community and the existing token format.

## Endpoint summary

| Method | Endpoint | Auth/role | Purpose |
|---|---|---|---|
| POST | `/api/v1/Communities/{communityId}/teachers/invite` | Bearer; Active Owner of route community | Create or resend the one-community teacher invitation. |
| GET | `/api/v1/community-invitations/teacher/validate?token=...` | Anonymous | Load safe setup-page context without activating membership. |
| POST | `/api/v1/Account/community-register` | Anonymous | Set name/password and activate an existing pending Owner or Teacher membership once. |
| POST | `/api/v1/Account/community-login` | Anonymous | Shared password login for a Platform Admin, Community Admin, or Teacher by normalized username or email. |
| POST | `/api/v1/Account/forgot-password` | Anonymous | Enumeration-safe reset-email request. |
| POST | `/api/v1/Account/reset-password` | Anonymous | Reset password with Identity token and revoke refresh sessions. |
| POST | Existing refresh endpoint | Existing contract | Preserve token rotation. |
| POST | Existing logout endpoint | Existing contract | Preserve token revocation/logout. |

## Removed endpoints

- Public teacher registration.
- Public email confirmation.
- Resend email confirmation.
- Authenticated teacher invitation acceptance.

They must return no mapped application action and must be absent from Swagger. Identity token-provider and email configuration remain because reset and invitation email still require them.

## Authorization and backend trust

- The invite route uses the route community ID but accepts no role or owner identity from the request body. The backend derives the caller from bearer claims and transactionally rechecks an Active Owner membership and active community.
- Validation and completion derive user, email, membership, and community entirely from the token hash and database state.
- Community login derives the role and community from persisted Owner/Teacher memberships and fails when there is zero or more than one current valid relationship. The public role values are `CommunityAdmin` for stored Owner and `Teacher` for stored Teacher; the client supplies neither a role nor a community.
- Google, player, admin, and owner authentication behavior is unchanged.

## Common errors

| HTTP | Stable code | Meaning / frontend use |
|---:|---|---|
| 400 | `InvalidTeacherInvitation` | Show one generic invalid/expired/used invitation state with a route back to support/login; do not distinguish token state. |
| 400 | `InvalidOrExpiredPasswordResetToken` | Show one safe reset-link error; allow requesting another reset. |
| 409 | `TeacherAlreadyBelongsToAnotherCommunity` | Owner invite or completion found another active/pending community reservation. Do not offer a silent move. |
| 401/400 per existing convention | Existing generic credential error | Login failed; do not state whether identifier, password, account, or community was responsible. |
| 403 | Existing authorization code | Caller is not allowed to invite for the route community. |

The implemented stable `ErrorCode` values preserve existing values: `TeacherAlreadyBelongsToAnotherCommunity = 4`, `InvalidTeacherInvitation = 5`, and `InvalidOrExpiredPasswordResetToken = 6`.

## Swagger acceptance

- Retired routes are absent.
- Invite request no longer contains `name`.
- Validate and complete appear as anonymous actions with their documented success/error responses.
- Login shows `identifier` and singular `community`.
- Forgot password documents HTTP 202 and its fixed generic response.
- Reset password shows `userId`, `token`, and `newPassword`.
- Google operations and schemas have no diff.

## Frontend usage notes

- Treat invitation email as display-only; the API never accepts an edited email during validation/completion.
- URL-encode raw invitation and reset tokens when navigating or sending requests. Never put tokens in analytics or application logs.
- After completion, redirect to login; there is no token or logged-in state to consume.
- Do not build community selection for teacher password login; the response contains exactly one community.
