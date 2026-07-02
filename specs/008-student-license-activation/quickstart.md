# Quickstart: Student License Activation on Login

## Prerequisites

- Google authentication is configured with allowed client ids.
- A valid Google ID token is available for manual login testing.
- Owner student license management has already created `StudentLicenses`.
- Database has at least one community with license capacity, grade, class, and a Pending student license.

## Build and Test Commands

```bash
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln
```

## Scenario 1: Pending Student License Activates on Login

1. Create or identify a Pending student license for `student@example.com`.
2. Confirm the license has `UserId = null`, `PlayerProfileId = null`, `Status = Pending`, and `ActivatedAt = null`.
3. Record `CommunityLicense.UsedStudents` for the license community.
4. Complete Google login with `student@example.com`.

Expected outcome:

- Login succeeds with the existing JWT, `userId`, and `playerProfileId` response fields.
- The pending license becomes Active.
- The license has `UserId` set to the resolved user.
- The license has `PlayerProfileId` set to the resolved or created player profile.
- The license has `ActivatedAt` set.
- An Active Student `CommunityUser` exists for the same community and user.
- `CommunityLicense.UsedStudents` is unchanged.

## Scenario 2: Existing Removed Student Access Is Restored

1. Create a Pending student license for `student-restore@example.com`.
2. Create a matching `CommunityUser` for the same community/user with `Role = Student` and `Status = Removed`.
3. Complete Google login with `student-restore@example.com`.

Expected outcome:

- The license becomes Active and links to user/player profile.
- The existing membership is updated to `Status = Active`.
- No duplicate `CommunityUser` row is created.
- `UsedStudents` is unchanged.

## Scenario 3: Repeated Login Is Idempotent

1. Run Scenario 1 once and record license status, membership row count, and `UsedStudents`.
2. Complete Google login with the same Google account again.

Expected outcome:

- Login succeeds normally.
- No new student membership row is created.
- `UsedStudents` is unchanged.
- The already Active license is not reactivated.

## Scenario 4: Revoked Licenses Are Ignored

1. Create or identify a Revoked student license for `revoked-student@example.com`.
2. Complete Google login with `revoked-student@example.com`.

Expected outcome:

- Login succeeds normally.
- The Revoked license remains Revoked.
- No Student membership is created from the Revoked license.
- `UsedStudents` is unchanged.

## Scenario 5: Multiple Communities Activate Together

1. Create Pending student licenses for the same email in two different communities.
2. Complete Google login with that email.

Expected outcome:

- Both licenses become Active.
- Both licenses link to the same resolved user and player profile.
- Active Student memberships exist in both communities.
- Neither community's `UsedStudents` changes during activation.

## Scenario 6: Existing Teacher Activation Still Works

1. Create a Pending Teacher membership for the login user.
2. Create a Pending student license for the same Google email.
3. Complete Google login.

Expected outcome:

- Existing Pending Teacher membership becomes Active.
- Pending student license becomes Active.
- Existing login response fields remain present.
- Teacher and student seat counters do not increase during activation.

## Manual API Call

```bash
curl -X POST "https://localhost:5001/api/v1/Account/google-login?googleAccessToken=GOOGLE_ID_TOKEN"
```

Use the actual local API base URL and a valid Google ID token for the configured environment.

## Notes

- This feature does not add frontend actions or a separate accept-invitation endpoint.
- The frontend continues to call the existing Google login endpoint.
- Activation side effects should be verified through database state or existing user/community APIs after login.
