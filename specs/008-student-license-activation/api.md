# Student License Activation on Login API

## Feature Summary

This feature extends the existing Google login flow so a student with Pending student licenses activates those licenses automatically when signing in with the matching Google email. Activation links the license to the resolved `User` and `Player` profile, then creates or restores Active Student community access.

No new endpoint, new request body, manual invitation acceptance flow, email sending, dashboard, analytics, payment logic, or schema change is included.

## Endpoint List

| Method | Route | Authentication | Purpose |
|---|---|---|---|
| POST | `/api/v1/Account/google-login` | Public; valid Google ID token required | Existing Google login plus pending student license activation side effect |

## POST /api/v1/Account/google-login

### Auth Requirements

The route is publicly callable but requires a valid Google ID token for the configured allowed client IDs. The backend resolves or creates the SprintLabs `User` from the Google email.

### Role/Permission Requirements

No existing community role is required before login. Pending student licenses are matched by normalized email. Only licenses with `Status = Pending` are activated.

### Request

The implemented controller binds the token from the query string.

```http
POST /api/v1/Account/google-login?googleAccessToken=GOOGLE_ID_TOKEN
```

No JSON request body is accepted for this route.

### Success Response

The response contract is unchanged from the existing login response.

```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzUxMiIs...",
    "userId": 42,
    "playerProfileId": 18,
    "name": "Student Name",
    "email": "student@example.com",
    "pictureUrl": "https://example.com/avatar.png",
    "gold": 0,
    "experience": 0,
    "level": 1
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

### Activation Side Effects

When matching Pending student licenses exist:

- `StudentLicense.UserId` is set to the resolved login user.
- `StudentLicense.PlayerProfileId` is set to the existing or created player profile.
- `StudentLicense.Status` changes from `Pending` to `Active`.
- `StudentLicense.ActivatedAt` is set.
- Active Student `CommunityUser` access is created if missing.
- Removed Student access is restored to Active.
- Existing Active Student access is not duplicated.
- `CommunityLicense.UsedStudents` is not incremented.

Revoked and already Active student licenses are ignored.

### Common Errors

| HTTP | Typical condition | Response behavior |
|---|---|---|
| 400 | Missing token, Google validation failure surfaced globally, or user update/create failure | Existing global/controlled error response |
| 401 | Google identity is missing required email or Google subject | `InvalidAccessToken` controlled error |
| 403 | Resolved user is suspended | `InvalidAccessToken` controlled error |
| 409 | Existing player profile is linked to a different user | Existing controlled conflict response |

## Frontend Usage Notes

- Keep calling the existing Google login route; no separate student activation call exists.
- Do not expect new response fields for activated communities or licenses.
- After login, refresh authenticated user/community state if the UI needs to show newly activated Student community access.
- Treat activation as a backend side effect: the login response is still the source for JWT, `userId`, and `playerProfileId`.
- Pending teacher activation continues through the same login flow.

## Manual Test Steps

1. Create a Pending student license for `student@example.com` through owner student-license management.
2. Record the community's `UsedStudents` value.
3. Call `POST /api/v1/Account/google-login?googleAccessToken=...` with a Google token for `student@example.com`.
4. Confirm the login response still includes `accessToken`, `userId`, and `playerProfileId`.
5. Confirm the matching student license is Active and has `UserId`, `PlayerProfileId`, and `ActivatedAt`.
6. Confirm an Active Student `CommunityUser` exists for the license community and login user.
7. Confirm `UsedStudents` did not change during login.
8. Repeat login and confirm no duplicate `CommunityUser` is created.

## Implementation Notes / Spec Differences

- The implemented route is `/api/v1/Account/google-login`, not the unversioned `/api/auth/google-login` wording in the feature brief.
- Student activation is intentionally a side effect of login and does not change the response contract.
- Existing non-Student community memberships are not overwritten during activation.

## Open Questions

- Should a future response include activated community summaries, or should the frontend always refresh memberships after login?
