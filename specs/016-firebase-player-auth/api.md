# Firebase Player Authentication API

## Feature Summary

This feature adds Firebase registration/login for Unity players without replacing Google login. Firebase and Google both finish through one shared player-login workflow, but each provider keeps its own verified identity and compatibility policy. Firebase UID is stored only on User; Player ownership and profile updates use internal UserId.

The canonical payload and error examples are in [contracts/firebase-player-authentication-api.md](./contracts/firebase-player-authentication-api.md).

## Route Summary

| Method | Route | Auth / role | Purpose |
|---|---|---|---|
| POST | `/api/v1/Account/firebase-login` | Public; Firebase ID token body | Register or log in Unity player |
| POST | `/api/v1/Account/google-login` | Existing Google token input | Existing Google player login |
| PUT | `/api/v1/Account/profile` | SprintLabs bearer token | Update current player through legacy route |
| PATCH | `/api/v1/PlayerProfiles/me` | SprintLabs bearer token | Update current player through current route |

No community role, CommunityUser, or StudentLicense is required for Firebase B2C login.

## Firebase Login Request

```json
{
  "idToken": "firebase-id-token"
}
```

The Unity client obtains this token from the signed-in Firebase user and sends it over HTTPS. It must not send Firebase UID, email verification, user ID, player ID, progression, or membership data separately.

## Firebase Login Success

```json
{
  "data": {
    "accessToken": "eyJ...",
    "userId": 123,
    "playerProfileId": 456,
    "name": "Player One",
    "email": "player@example.com",
    "pictureUrl": "",
    "gold": 0,
    "experience": 0,
    "level": 1
  },
  "message": "Success",
  "statusCode": 200,
  "errorCode": 1
}
```

Frontend usage notes:

- Store/use `accessToken` for SprintLabs API calls.
- Treat `userId` and `playerProfileId` as response identifiers, not client-authoritative ownership input.
- Repeated Firebase login returns the same IDs and current progression.
- Empty `pictureUrl` is valid.
- A user with no community state is a valid successful B2C login.

## Common Errors

- `401`: missing, malformed, expired, revoked, incomplete, incorrectly signed, or wrong-project Firebase token.
- `403`: the resolved SprintLabs user is suspended.
- `409`: trusted external identities disagree, the selected account already has another Firebase UID/provider identity, or a legacy player belongs to another user.

For `401`, the client may refresh the Firebase ID token once if the Firebase session is still valid, then retry once. It must not loop indefinitely. For `409`, do not create another Firebase/SprintLabs account automatically; show account-conflict support guidance.

## Linking and Data Ownership

- Firebase UID is the first user lookup key.
- Verified Google provider identity is the second.
- Verified normalized email is the third.
- Unverified email cannot claim existing data.
- Conflicting identities return 409 without a merge.
- Player lookup starts with internal UserId.
- Firebase UID is never stored in Player.GoogleId.

## Login-Time Business Behavior

- Suspended users do not receive tokens.
- Pending student licenses continue activating under existing rules.
- Firebase login activates only eligible pending Teacher state and records invitation acceptance consistently when a usable invitation exists.
- Invalid invitation state does not activate.
- B2C login creates no membership or license.
- Repeated activation does not change seat usage or create duplicates.

## Google Compatibility

- Keep calling the existing Google endpoint exactly as before.
- Its public request and `BaseResponse<LoginResponse>` remain unchanged.
- Existing GoogleId values are not rewritten as Firebase UIDs.
- Existing Google player and student activation behavior remains operational.
- Google login continues leaving pending Teacher memberships pending.

## Profile Update Ownership

Both player profile update routes use the SprintLabs access token's internal `userId` claim:

- `PUT /api/v1/Account/profile`
- `PATCH /api/v1/PlayerProfiles/me`

Do not send a user ID, GoogleId, FirebaseUid, provider subject, or player owner ID in either body. Missing/invalid internal `userId` is unauthorized.

## Loading, Empty, and Error Notes

- Disable duplicate Firebase login submissions while one request is in flight.
- A successful response with no community membership is not an empty/error state.
- Preserve the local Firebase session separately from the SprintLabs access token lifecycle.
- Never put either token in URLs, analytics, crash reports, console output, or application logs.
- Clear the SprintLabs session on unrecoverable `401`/`403`.
- Preserve Firebase session/account state on `409` so support can diagnose without prompting duplicate registration.

## Open Questions

- SprintLabs access-token refresh for player sessions remains outside this feature; clients continue using the existing player token lifetime/reauthentication behavior.
