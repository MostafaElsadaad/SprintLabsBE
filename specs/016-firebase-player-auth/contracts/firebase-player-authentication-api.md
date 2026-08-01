# HTTP Contract: Firebase Player Authentication

## Conventions

- API version: `v1`.
- JSON properties use camel case.
- Firebase login is public at the HTTP layer; the Firebase ID token authenticates the request.
- Successful login uses the existing `BaseResponse<LoginResponse>`.
- Controlled errors use the existing BaseResponse error fields and never echo the submitted token or provider error.
- Subsequent player APIs use the returned SprintLabs access token as `Authorization: Bearer <accessToken>`.

## Endpoint Matrix

| Method | Route | Authentication | Success |
|---|---|---|---:|
| POST | `/api/v1/Account/firebase-login` | Firebase ID token in JSON body | 200 |
| POST | `/api/v1/Account/google-login?googleAccessToken=...` | Existing Google token query input | 200; unchanged |
| PUT | `/api/v1/Account/profile` | SprintLabs bearer token with `userId` | 200; existing route corrected |
| PATCH | `/api/v1/PlayerProfiles/me` | SprintLabs bearer token with `userId` | 200; unchanged |

## POST /api/v1/Account/firebase-login

Request:

```json
{
  "idToken": "firebase-id-token"
}
```

Request rules:

- `idToken` is required and must be non-empty.
- It must be a Firebase client ID token from the configured Firebase project.
- A Firebase custom token, Google ID token sent directly, SprintLabs access token, or token from another Firebase project is not accepted.
- The request must not include UserId, PlayerProfileId, FirebaseUid, GoogleId, email-verification status, progression values, or community state.

Success:

```json
{
  "data": {
    "accessToken": "eyJ...",
    "userId": 123,
    "playerProfileId": 456,
    "name": "Player One",
    "email": "player@example.com",
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

Success behavior:

- First valid login creates one SprintLabs User and one Player when no safe existing records resolve.
- Repeated login returns the same UserId and PlayerProfileId.
- Existing progression and editable profile data are preserved.
- `pictureUrl` may be an empty string when Firebase provides no picture.
- `name` falls back to the verified email when Firebase provides no display name.
- B2C login succeeds with no CommunityUser or StudentLicense.
- Eligible post-login activation completes before the SprintLabs token is returned.

Errors:

| HTTP | Condition | Data changes |
|---:|---|---|
| 401 | Missing/blank/malformed/expired/revoked/wrong-project token | None |
| 401 | Verified token lacks Firebase UID or email | None |
| 403 | Resolved SprintLabs user is suspended | None after suspension detection; no token |
| 409 | Firebase UID, Google provider ID, or verified email point to different users | None |
| 409 | Selected user already has a different FirebaseUid or conflicting GoogleId | None |
| 409 | Legacy player is ambiguous or belongs to another user | None |

Example controlled error:

```json
{
  "data": null,
  "message": "InvalidAccessToken",
  "statusCode": 401,
  "errorCode": 3
}
```

Conflict example:

```json
{
  "data": null,
  "message": "Record Already Exists",
  "statusCode": 409,
  "errorCode": 3
}
```

The exact existing BaseResponse serialization is authoritative. No error contains the Firebase token, decoded claim set, Firebase exception, credential path, project secret, or service-account data.

## Identity Linking Contract

Resolution order:

1. Exact Firebase UID.
2. Verified Google provider identity carried by the verified Firebase token.
3. Verified normalized email.

Rules:

- All available verified identifiers must agree with the selected user.
- Email is never a linking key when `email_verified` is false.
- An unverified email that collides with an existing user cannot create a duplicate and returns 409.
- A safe existing user receives FirebaseUid without changing a nonconflicting GoogleId.
- Conflicting identities are not merged automatically.
- Firebase-only players keep `Player.GoogleId = null`.

## Login-Time Activation Contract

- Pending student licenses continue using existing email, role, status, and seat rules for both Google and Firebase completion workflows.
- Firebase login additionally activates eligible pending Teacher state for the resolved user.
- A current invitation must match the verified email and be usable; successful login activation records acceptance and does not change UsedTeachers.
- Legacy pending Teacher memberships without invitation records may activate for the already-linked user.
- Invalid, expired, revoked, superseded, mismatched, other-user, or non-Teacher invitation state remains unchanged.
- Google login retains its current behavior: pending Teacher memberships remain pending until explicit acceptance.

## Existing Google Login Compatibility

No public contract change:

```http
POST /api/v1/Account/google-login?googleAccessToken={token}
```

- Input remains the existing query parameter.
- Success remains `BaseResponse<LoginResponse>`.
- Existing Google user/player creation and reuse continue.
- Existing student-license activation continues.
- Pending Teacher memberships remain pending.
- GoogleId remains the Google subject; FirebaseUid is not required.

## PUT /api/v1/Account/profile

Existing authenticated route and body remain:

```json
{
  "name": "Player One",
  "schoolName": "School",
  "grade": 5,
  "age": 10
}
```

Ownership correction:

- The server selects the Player using the SprintLabs JWT `userId` claim.
- The request cannot provide the owning user.
- GoogleId, FirebaseUid, JWT `sub`, and `ClaimTypes.NameIdentifier` do not select the profile.
- A missing/unparseable `userId` returns 401.
- Existing field validation, not-found behavior, suspended-user protection, and response shape are preserved.

## PATCH /api/v1/PlayerProfiles/me

No contract change. It already scopes the profile by the SprintLabs JWT `userId`.

## Access Token Claims

Successful external player login retains:

- provider subject as `sub`;
- email;
- name;
- internal `userId`;
- internal `playerProfileId`.

Only `userId` is authoritative for user/profile ownership. Provider subject values may differ between Google and Firebase.
