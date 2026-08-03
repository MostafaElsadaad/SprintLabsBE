# Frontend Handoff: Invite-Only Teacher Authentication

Frontend implementation is out of scope for this backend feature. This document defines only the pages, fields, states, and API calls required to consume the changed backend safely.

## Owner teacher invitation section

**Visibility**: Active community Owner only. The backend remains authoritative even if the UI hides the action.

**Form fields**:

- Email: required, valid email shape, trimmed before submission.
- No teacher name field.

**Action**: `POST /api/v1/Communities/{communityId}/teachers/invite`.

**States**:

- Loading: disable duplicate submission and show progress.
- Success/new invite: confirm that the invitation was sent.
- Success/resend: confirm that a replacement invitation was sent; the earlier link is invalid.
- HTTP 409 `TeacherAlreadyBelongsToAnotherCommunity`: state that this email is already reserved by/belongs to another community; do not offer an automatic move.
- Authorization/capacity/validation: use the existing owner-management error presentation.

Existing teacher table columns and unrelated owner controls are unchanged by this feature.

## Invited-teacher setup page

**Route**: `/invitations/teacher/setup?token={token}`.

On load, call `GET /api/v1/community-invitations/teacher/validate?token={encodedToken}`.

**Display from successful validation**:

- Community name.
- Masked email (read-only display).
- Expiration time if the current UI presents expiry context.

**Form fields**:

- Name: required after trimming; do not submit whitespace-only values.
- Password: required and show the product's configured password guidance.
- Confirm password: frontend-only equality validation; omit it from the API request.
- Email: never editable and never sent in the completion body.

**Submit**: `POST /api/v1/community-invitations/teacher/complete` with `token`, `name`, and `password` only.

**States**:

- Initial validation loading: do not render an editable setup form until validation succeeds.
- Invalid/expired/revoked/used token: show one generic unusable-invitation state. The backend intentionally does not distinguish causes.
- Password/name validation: keep the form and show safe field/form errors.
- Submitting: disable repeated submission.
- Success: show completion and redirect to teacher login. Do not expect or store access/refresh tokens.
- HTTP 409: show that the teacher is already associated with another community and cannot complete this invitation.

Never send the raw token to telemetry, crash reports, analytics, or logs.

## Teacher login page

**Form fields**:

- Identifier: username or email.
- Password.

**Action**: `POST /api/v1/Account/teachers/login`.

**Success handling**:

- Store/use access and refresh tokens exactly as the existing client does.
- Consume singular `community`, not `communities` or a community selection flow.
- Do not expect player profile or student license data.

**Error handling**:

- Use one generic login failure for unknown identifier, invalid password, incomplete setup, pending/no/multiple community relationships, inactive community, suspension, and lockout.
- Do not infer account existence from timing or message content.

There is no public teacher registration link or email-confirmation/resend screen/action in the teacher password journey.

## Forgot-password page

**Form field**: Identifier (username or email).

**Action**: `POST /api/v1/Account/forgot-password`.

**Success handling**: For every HTTP 202 response, show exactly the generic acknowledgement: “If a matching account exists, password reset instructions have been sent.” Do not show whether the account exists, is eligible, is cooling down, or whether mail delivery succeeded.

**States**: normal validation, submitting, and the same accepted state for known and unknown identifiers. Avoid resend countdown claims unless they are purely presentational because the API does not disclose cooldown state.

## Reset-password page

**Route**: `/reset-password?userId={userId}&token={token}`.

**Form fields**:

- New password.
- Confirm new password (frontend-only equality check).

**Action**: `POST /api/v1/Account/reset-password` with `userId`, URL-safe token, and `newPassword`.

**States**:

- Submitting: prevent duplicates.
- Password-policy validation: show safe policy feedback returned under existing conventions.
- Invalid/expired token: show one generic link error and offer navigation to forgot password.
- Success: redirect to login. The backend revokes existing refresh sessions and does not automatically log in.

## Unchanged frontend areas

- Google login screens, requests, account linking, response handling, and claims assumptions.
- Refresh-token rotation and logout behavior.
- Player/Firebase, admin, and owner authentication.
- Player profiles and student licenses.
- Multi-community teacher selection (not supported and must not be added).
