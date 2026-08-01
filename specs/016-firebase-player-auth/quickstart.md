# Quickstart: Firebase Player Authentication

## Purpose

Validate Firebase registration/login, safe legacy linking, player defaults, activation behavior, profile ownership, and Google compatibility after implementation.

## Prerequisites

- .NET 8 SDK.
- MySQL reachable through `ConnectionStrings__DefaultConnection`.
- A Firebase project matching the Unity client configuration.
- A Cloud Run service identity with the required Firebase Authentication access, or a local ADC credential outside the repository.
- A valid Firebase test user and, for compatibility testing, an existing Google player.

Automated tests do not need Firebase credentials and must never contact Firebase.

## Configuration

Supply the Firebase project ID:

```powershell
$env:Authentication__Firebase__ProjectId = "your-firebase-project-id"
```

Cloud Run uses its attached service account through Application Default Credentials. No credential environment variable is normally needed there.

For local development only, point ADC to a credential file stored outside the repository:

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\secure\outside-repo\firebase-service-account.json"
```

Never copy that JSON file into the workspace, appsettings, test assets, build output, or source control.

## Package Verification

After implementation:

```powershell
dotnet restore SprintLabs.sln
dotnet list Infrastructure/Infrastructure.csproj package --include-transitive
```

Confirm:

- `FirebaseAdmin` resolves to 3.6.0.
- `Google.Apis.Auth` resolves to 1.75.0 with no downgrade warning.
- Only Infrastructure directly references FirebaseAdmin.

## Migration

Generate during implementation:

```powershell
dotnet ef migrations add AddFirebasePlayerAuthentication --project Infrastructure --startup-project API
```

Inspect before applying:

- nullable `Users.FirebaseUid` exists with max length 128;
- FirebaseUid has a unique index;
- existing Users receive null FirebaseUid;
- `Players.GoogleId` becomes nullable but retains its unique index;
- existing GoogleId values are unchanged;
- no table, membership, license, invitation, progression, or old migration is changed;
- Down migration does not delete players or copy Firebase UID into GoogleId.

Apply locally:

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

## Build and Automated Tests

```powershell
dotnet build SprintLabs.sln
dotnet test SprintLabs.Tests/Compass.Tests.csproj --filter "FullyQualifiedName~FirebasePlayerAuthentication|FullyQualifiedName~B2CPlayerProfileSupport|FullyQualifiedName~StudentLicenseActivation|FullyQualifiedName~OwnerTeacherManagement|FullyQualifiedName~AccountProfile"
dotnet test SprintLabs.sln
```

Automated test inspection:

- `IFirebaseAuthenticationService` is mocked.
- No test calls `FirebaseApp.Create`, `FirebaseAuth.DefaultInstance`, or `VerifyIdTokenAsync`.
- No test requires ADC, `GOOGLE_APPLICATION_CREDENTIALS`, or network access.

## Scenario 1: First Firebase B2C Login

1. Create/sign in a Firebase test user with an email not present in SprintLabs.
2. Obtain a fresh Firebase ID token through the Unity client/test harness.
3. Call:

```http
POST /api/v1/Account/firebase-login
Content-Type: application/json

{
  "idToken": "<fresh-firebase-id-token>"
}
```

4. Expect HTTP 200 with AccessToken, UserId, PlayerProfileId, Name, Email, PictureUrl, Gold 0, Experience 0, and Level 1.
5. Confirm one User has FirebaseUid and one Player has that UserId.
6. Confirm Player.GoogleId is null for a non-Google Firebase user.
7. Confirm no CommunityUser or StudentLicense was created.

## Scenario 2: Repeated Login and Progress Preservation

1. Change the player's progression/profile through existing supported flows or test data.
2. Log in again with the same Firebase identity.
3. Expect the same UserId and PlayerProfileId.
4. Confirm progression and age/grade/school data are unchanged.
5. Confirm user/player row counts did not increase.

## Scenario 3: Legacy Google Account Linking

1. Seed/use an existing User and Player with a GoogleId and progression.
2. Link/sign in that Google provider through the Firebase project.
3. Submit the Firebase ID token.
4. Expect the existing UserId and PlayerProfileId.
5. Confirm User.FirebaseUid is populated, User.GoogleId and Player.GoogleId remain the Google provider ID, and progression is unchanged.
6. Repeat and confirm idempotency.

## Scenario 4: Verified and Unverified Email

- Verified email with no UID/Google match and one safe existing email user: expect that user to link.
- Unverified email matching an existing user: expect 409 and no link.
- Unverified email with no existing collision: expect first registration only if all required identity data is present; confirm it does not claim legacy records by email.

## Scenario 5: Identity Conflicts

Test each:

- FirebaseUid points to user A, Google provider ID points to user B.
- FirebaseUid points to user A, verified email points to user B.
- Google provider ID points to user A, verified email points to user B.
- selected user already has another FirebaseUid.
- selected user has a conflicting GoogleId.
- legacy player candidate belongs to another user.

Expect HTTP 409, no new records, no changed external IDs, no activations, and no SprintLabs token.

## Scenario 6: Invalid Tokens

Submit:

- blank string;
- malformed value;
- expired token;
- revoked token;
- valid token from another Firebase project;
- Google ID token sent directly instead of a Firebase ID token.

Expect HTTP 401. Confirm the response/log does not contain the token, decoded claims, Firebase exception text, credential path, or service-account data.

## Scenario 7: Suspended User

1. Link a Firebase UID to a suspended User.
2. Submit a valid token.
3. Expect HTTP 403 and no SprintLabs token.
4. Confirm player, membership, license, and progression state is unchanged.

## Scenario 8: Student License Activation

1. Seed a Pending StudentLicense matching the verified Firebase email.
2. Log in through Firebase.
3. Confirm the license becomes Active and links UserId/PlayerProfileId.
4. Confirm Active Student membership is created/restored under existing rules.
5. Confirm UsedStudents does not increase.
6. Repeat login and confirm no duplicates or counter changes.

## Scenario 9: Teacher Activation

1. Seed a Pending Teacher membership for the resolved user with a matching usable invitation and reserved teacher seat.
2. Log in through Firebase with verified matching email.
3. Confirm membership becomes Active, the invitation receives AcceptedAt, and UsedTeachers is unchanged.
4. Repeat and confirm idempotency.
5. Repeat with expired, revoked, superseded, mismatched, other-user, and non-Teacher invitation states; each remains unchanged.
6. Seed a legacy Pending Teacher membership with no invitation record; confirm Firebase login activates it without changing seat usage.
7. Run the same pending Teacher case through Google login; confirm it remains Pending.

## Scenario 10: Profile Ownership

1. Use a Firebase-issued SprintLabs access token whose `sub` is not a Google ID.
2. Call `PUT /api/v1/Account/profile` and `PATCH /api/v1/PlayerProfiles/me`.
3. Confirm both update the Player attached to internal `userId`.
4. Remove `userId` or provide only `sub`; expect 401 and no update.
5. Confirm no lookup uses FirebaseUid or GoogleId.

## Scenario 11: Google Regression

1. Call the existing Google login endpoint with its existing query parameter.
2. Confirm the response envelope and all LoginResponse fields are unchanged.
3. Confirm new/existing Google player creation/reuse works.
4. Confirm pending student licenses still activate.
5. Confirm pending Teacher memberships remain Pending until the existing explicit acceptance flow.
6. Confirm FirebaseUid is not required for Google-only users.

## Concurrency Checks

- Send two first-logins for one Firebase UID: one User and one Player remain.
- Send two different Firebase UIDs with one verified email: at most one safe link/create succeeds; the conflicting request returns 409.
- Send concurrent Firebase login for one legacy player: it ends linked to one UserId only.
- Send concurrent teacher activation: one Active membership and one AcceptedAt result; seat usage is unchanged.

## Security Inspection

- `git grep` finds no service-account private key/header or committed credential JSON.
- Tracked settings contain project ID configuration only.
- Logs contain no request bodies or Firebase/SprintLabs tokens.
- Provider exceptions are mapped before global exception middleware.
- Firebase SDK namespaces appear only in Infrastructure.
- Profile code queries by UserId, not provider subject.

See [the data model](./data-model.md), [HTTP contract](./contracts/firebase-player-authentication-api.md), [API guide](./api.md), and [frontend guide](./frontend.md).
