# Firebase Player Authentication Frontend Requirements

## Scope

The Unity client owns Firebase registration/sign-in and obtains a Firebase ID token. This backend feature accepts that token and returns a SprintLabs session. Unity implementation is outside this repository; this document defines the supported client flow, fields, states, and API usage.

## Required Pages or Sections

- Player registration/sign-in using configured Firebase providers.
- Login progress state while Firebase and SprintLabs authentication complete.
- Account-conflict state for HTTP 409.
- Invalid/expired session state for HTTP 401.
- Suspended-account state for HTTP 403.
- Existing player home/profile experience after successful SprintLabs login.
- Existing Google login entry point remains available wherever currently exposed.

## User Actions

1. Register or sign in through the Firebase Unity SDK.
2. Obtain a fresh Firebase ID token from the signed-in Firebase user.
3. Submit it to `POST /api/v1/Account/firebase-login`.
4. Store the returned SprintLabs access token using the client's existing secure session strategy.
5. Continue into B2C play even when no community membership or student license exists.
6. Update the player's own profile through an existing current-profile endpoint.

## Forms and Fields

Firebase provider forms are managed by the Unity/Firebase integration. The SprintLabs request has one field:

| Field | Type | Required | Notes |
|---|---|---:|---|
| idToken | string | Yes | Fresh Firebase ID token; never display or log |

Do not add editable fields for FirebaseUid, GoogleId, UserId, PlayerProfileId, email verification, Gold, Experience, Level, membership, or license state.

## Validation Rules

- Do not call SprintLabs until Firebase sign-in succeeds and returns a non-empty ID token.
- Send the token only in the HTTPS JSON body.
- Do not attempt to decode token claims to decide SprintLabs ownership or authorization.
- Treat backend HTTP status as authoritative.
- Never use unverified client email/provider data to preselect or merge SprintLabs accounts.

## Loading and Success States

- Show one combined sign-in progress state across Firebase token acquisition and SprintLabs login.
- Disable duplicate submissions while the flow is active.
- On success, use the returned Name, PictureUrl, Gold, Experience, and Level to initialize the player UI.
- Empty PictureUrl uses the existing default avatar.
- Zero communities/licenses is a valid B2C success; do not show a licence-required error.
- Repeated login should feel like normal sign-in and must display current, not defaulted, progression.

## Error States

| HTTP/state | Client behavior |
|---|---|
| Firebase client sign-in failure | Show Firebase-safe sign-in guidance; do not call backend without a token |
| 401 | Ask Firebase for one fresh ID token and retry once; if still failing, end the SprintLabs login attempt |
| 403 | Show suspended-account handling and do not enter the game |
| 409 | Show account conflict/support guidance; do not create another account or retry in a loop |
| Network/5xx | Show retryable service-unavailable state without logging token/request body |

Provider or backend raw exception text must never be shown. Token values must not appear in screenshots, telemetry, analytics, crash reporting, or support copy.

## Permissions and Visibility

- Firebase player login is public at the SprintLabs HTTP layer.
- No CommunityUser role or StudentLicense is required to show/use B2C login.
- Community UI remains controlled by active membership returned/fetched through existing APIs.
- Suspended users cannot enter authenticated player UI.
- Google login visibility and behavior remain unchanged.
- A successful identity link is transparent; the player continues with the same UserId, PlayerProfileId, and progression.

## Profile Updates

- Use an existing current-player route after storing the SprintLabs token.
- Do not send or infer the owner from FirebaseUid, GoogleId, Firebase subject, or player subject.
- The backend selects the player from the internal `userId` claim in the SprintLabs token.
- Preserve existing form fields and validation for name, age, grade, and school name according to the chosen existing route.

## API Calls by Action

| User action | API |
|---|---|
| Complete Firebase registration/login | `POST /api/v1/Account/firebase-login` |
| Existing Google login | Existing `POST /api/v1/Account/google-login` call |
| Legacy profile save | `PUT /api/v1/Account/profile` |
| Current profile save | `PATCH /api/v1/PlayerProfiles/me` |
| Load current profile | Existing `GET /api/v1/Users/me/player-profile` |

## Client Session Notes

- Firebase ID token authenticates only the Firebase login exchange.
- SprintLabs access token authenticates subsequent SprintLabs API requests.
- Do not substitute one token for the other.
- Do not retain the Firebase ID token longer than needed for the exchange/retry.
- Existing SprintLabs player token expiry and reauthentication behavior remains unchanged.

## Open Questions

- Which Firebase providers and exact Unity registration screens are enabled is a Unity/product configuration decision outside this backend feature.
- Secure token storage depends on the supported Unity target platforms and remains a client implementation decision.
