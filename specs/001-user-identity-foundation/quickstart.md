# Quickstart: User Identity Foundation

## Prerequisites

- MySQL connection configured in the API settings.
- Google authentication settings configured with allowed client ids.
- A valid Google ID token for manual login validation.
- Existing project dependencies restored.

## Implementation Validation Commands

Run after implementation:

```powershell
dotnet build SprintLabs.sln
```

Run tests when feasible:

```powershell
dotnet test SprintLabs.sln
```

If database-backed tests require local MySQL and cannot run in the current environment, document that limitation and still run the build.

## Database Validation

1. Generate the EF Core migration for this feature.
2. Verify the migration:
   - Adds or extends the `Users` table fields for GoogleId, Name, AvatarUrl, IsPlatformAdmin, Status, CreatedAt, and UpdatedAt.
   - Adds nullable `Players.UserId`.
   - Adds unique `Users.Email`.
   - Adds optional index on `Users.GoogleId`.
   - Adds unique nullable index on `Players.UserId`.
   - Adds a foreign key from `Players.UserId` to `Users.Id`.
3. Apply migration to a development database.
4. Confirm existing player rows remain valid when `Players.UserId` is null.

## API Validation Scenarios

### New Google Player Login

1. Call `POST /api/v1/Account/google-login?googleAccessToken={token}` with a Google account not present in `Users`.
2. Expect a successful `BaseResponse<LoginResponse>`.
3. Confirm response includes:
   - `accessToken`
   - `userId`
   - `playerProfileId`
   - existing player progression fields
4. Confirm database contains one matching user and one linked player profile.

### Repeated Google Login

1. Call the same Google login again with the same Google email.
2. Expect the same `userId`.
3. Expect no duplicate user.
4. Expect the same or correctly linked `playerProfileId`.

### Current User

1. Use the login token as a bearer token.
2. Call `GET /api/v1/Users/me`.
3. Expect user id, email, name, avatar, status, platform-admin flag, and player profile id when present.

### Current Player Profile

1. Use the login token as a bearer token.
2. Call `GET /api/v1/Users/me/player-profile`.
3. For a linked player, expect that player's profile only.
4. For a user without a player profile, expect a controlled not-found response.

### Security Checks

1. Call both `/me` endpoints without a bearer token and expect unauthorized.
2. Use a token for one user and confirm no other user's profile can be returned.
3. Mark a test user as Suspended and confirm authenticated identity actions are blocked with controlled errors.

## Out-of-Scope Verification

Confirm the implementation does not add:

- Communities
- Community users
- Licenses
- Teachers
- Owners
- Admin community management
- Grades/classes beyond existing player profile fields
- Student licenses
