# Quickstart: Teacher Email Authentication

## Purpose

Validate the complete teacher registration, confirmation, password session, recovery, invitation, and seat-accounting flow after implementation.

## Prerequisites

- .NET 8 SDK.
- MySQL database reachable through `ConnectionStrings__DefaultConnection`.
- Current migrations applied.
- JWT secret supplied through user secrets or environment configuration; do not place it in tracked JSON.
- Mailpit running locally or a Mailtrap SMTP inbox.
- A community with an Active Owner membership and available teacher capacity.

## Development Configuration

Non-secret Mailpit-compatible settings:

```json
{
  "EmailOptions": {
    "Host": "localhost",
    "Port": 1025,
    "EnableSsl": false,
    "FromEmail": "no-reply@sprintlabs.local",
    "FromName": "SprintLabs"
  },
  "FrontendOptions": {
    "BaseUrl": "http://localhost:3000"
  }
}
```

Mailpit inbox: `http://localhost:8025`.

Keep JWT signing key, database password, and hosted SMTP credentials in environment variables or .NET user secrets.

## Migration Inspection

Generate the migration from the repository root during implementation:

```powershell
dotnet ef migrations add TeacherEmailAuthentication --project Infrastructure --startup-project API
```

Inspect the generated migration before applying it:

- User confirmation/lockout fields are added with safe defaults.
- `IsTeacherAccount` and `LastConfirmationEmailSentAt` are added.
- existing Teacher members are backfilled as teacher accounts.
- normalized email uniqueness is enforced.
- `RefreshTokens` and `TeacherInvitations` have unique hash indexes and required foreign keys.
- `CommunityUsers.CommunityId` remains non-nullable.
- no Player, StudentLicense, grade, class, or progression schema is changed.

Apply locally:

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

## Build and Automated Tests

Run the smallest gate first:

```powershell
dotnet build SprintLabs.sln
```

Then run focused tests:

```powershell
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~TeacherEmailAuthentication|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~B2CPlayerProfileSupport|FullyQualifiedName~StudentLicenseActivation"
```

Run the full suite:

```powershell
dotnet test SprintLabs.sln
```

If optional MySQL integration tests require a local instance, report that limitation separately; build and provider-independent tests must still pass.

## Scenario 1: Standalone Registration and Confirmation

1. Register a new teacher:

```http
POST /api/v1/Account/teachers/register
Content-Type: application/json

{
  "name": "Jane Smith",
  "email": "jane@example.com",
  "password": "StrongPass123!"
}
```

2. Expect `202`, a confirmation message, and `resendAvailableAt`.
3. Confirm in the database that:
   - one User exists and is teacher-marked;
   - email is unconfirmed;
   - PasswordHash is populated;
   - no Player, CommunityUser, or StudentLicense was created.
4. Open Mailpit, copy the confirmation URL, and submit its `userId` and `token` to the confirm endpoint.
5. Expect `200`; repeat and expect safe success.
6. Confirm no community membership was activated.

## Scenario 2: Confirmation Cooldown

1. Request resend twice inside 60 seconds.
2. Expect the generic `202` response both times.
3. Confirm only the allowed send appears in Mailpit and `LastConfirmationEmailSentAt` persists.
4. Restart the API and repeat before cooldown expiry; expect no additional email.
5. Repeat after expiry; expect a new message.
6. Compare unknown and already-confirmed email responses; neither should disclose account existence.

## Scenario 3: Invited Placeholder Registration

1. As an Active Owner, invite a new email.
2. Confirm one Pending Teacher membership, one used teacher seat, one teacher-marked passwordless User, and one invitation email.
3. Register with that email.
4. Confirm the same User ID is reused, PasswordHash is added, name is updated, and membership remains Pending.
5. Confirm no Player or StudentLicense is created.

## Scenario 4: Existing Google/Player Conflict

1. Seed or sign in a Google/player identity with `IsTeacherAccount=false`.
2. Attempt teacher registration with the same normalized email.
3. Expect `409`.
4. Confirm there is still one User, no password was added, and Google player login still succeeds.

## Scenario 5: Login With Zero and Multiple Communities

1. Attempt login before email confirmation; expect denial and no tokens.
2. Confirm the email and log in with correct credentials.
3. Expect access/refresh tokens and `communities: []`.
4. Add Active, Pending, and Removed memberships across communities.
5. Log in again; expect only all Active memberships.
6. Confirm no community-role claim appears in the JWT.
7. Confirm wrong passwords increment failed access and the sixth attempt during lockout remains blocked.

## Scenario 6: Refresh Rotation and Logout

1. Use a login refresh token once; expect a new token pair.
2. Reuse the old refresh token; expect `401`.
3. Run two concurrent refresh requests with the same current token; expect exactly one success.
4. Logout with the active token; refresh must fail.
5. Repeat logout; expect safe `200`.
6. Inspect persistence and confirm only SHA-256 hashes, not submitted raw values, are stored.

## Scenario 7: Forgot and Reset Password

1. Request forgot-password for existing, unknown, and non-teacher emails.
2. Expect the exact same public message for each.
3. Use the valid reset link from Mailpit with a compliant new password.
4. Expect success; old password must fail and new password must work.
5. Every refresh token issued before reset must fail.
6. Repeat with expired, malformed, and already-used reset tokens; password must remain unchanged.

## Scenario 8: Invitation Reissue and Acceptance

1. Record `UsedTeachers`, then invite a teacher.
2. Expect Pending status and `UsedTeachers + 1`.
3. Reissue the same Pending invitation.
4. Expect unchanged membership ID and seat count; the old link must fail.
5. Register/confirm/login as the invited teacher.
6. Confirm login alone leaves the membership Pending.
7. Accept the newest invitation using the teacher access token.
8. Expect Active membership details and unchanged `UsedTeachers`.
9. Repeat acceptance; expect the same Active membership and unchanged seat count.
10. Call `GET /api/v1/Users/me/communities`; the community must now appear.

## Scenario 9: Invitation Authorization and Isolation

- Accept with another account: `403`, no changes.
- Accept with matching but unconfirmed teacher: `403`, no changes.
- Accept expired/revoked/superseded token: `410`, no changes.
- Accept after membership removal: fail; no reactivation.
- Invite as Teacher, Student, Pending Owner, Removed Owner, or platform admin without Active Owner membership: `403`.
- Accept independent invitations into two communities: both memberships become Active; each seat was counted only at its own invitation.

## Scenario 10: Removal and Compatibility

1. Remove a Pending teacher and confirm the outstanding invitation is revoked and one seat released.
2. Repeat removal and confirm no second decrement.
3. Remove an Active teacher and confirm community access ends.
4. Run existing Google login compatibility tests:
   - player creation/reuse remains;
   - student-license activation remains;
   - pending Teacher memberships do not auto-activate;
   - existing Active Teacher memberships remain active.

## Security Inspection

- Startup output contains no JWT secret.
- Tracked appsettings contain no JWT, SMTP, or database credential introduced by this feature.
- JWT validation rejects wrong issuer, audience, signature, and expiry.
- Teacher JWT contains `jti`, `sub`, `userId`, and teacher account type but no community roles.
- Logs and controlled errors contain no passwords or raw confirmation, reset, refresh, or invitation tokens.
- Refresh and invitation database columns contain only hashes.

See [the data model](./data-model.md), [HTTP contract](./contracts/teacher-authentication-api.md), [API guide](./api.md), and [frontend guide](./frontend.md).

