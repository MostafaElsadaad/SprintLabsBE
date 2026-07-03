# Quickstart: B2C Player Profile Support

## Purpose

Validate that B2C players can log in, view, and update their player profile without community membership or student licenses.

Related artifacts:

- [spec.md](./spec.md)
- [plan.md](./plan.md)
- [data-model.md](./data-model.md)
- [contracts/b2c-player-profile-api.openapi.yaml](./contracts/b2c-player-profile-api.openapi.yaml)

## Prerequisites

- Existing SprintLabs solution restored.
- Database has existing migrations applied.
- Test data or mocks can represent:
  - A Google account with no CommunityUser and no StudentLicense.
  - An existing B2C User with PlayerProfile only.
  - Existing B2B student activation scenario.
  - Existing Teacher or Owner login scenario.

## Build and Test

From repository root:

```bash
dotnet build SprintLabs.sln
```

For implementation validation:

```bash
dotnet test SprintLabs.sln
```

If database-backed integration tests require a local MySQL instance and cannot run in the environment, record that limitation and still run `dotnet build SprintLabs.sln`.

## Manual Validation Scenarios

### 1. B2C Login Without CommunityUser

1. Use a Google account that has no `CommunityUser` record.
2. Call `POST /api/v1/Account/google-login?googleAccessToken=...`.
3. Confirm login succeeds.
4. Confirm response includes `userId`, `playerProfileId`, token, email, name, and progression fields.
5. Confirm no community membership was created.

### 2. B2C Login Without StudentLicense

1. Use a Google account with no `StudentLicense`.
2. Call the Google login endpoint.
3. Confirm login succeeds.
4. Confirm no student license was created.

### 3. Current Player Profile Read

1. Authenticate as the B2C player.
2. Call `GET /api/v1/Users/me/player-profile`.
3. Confirm response includes only the current player's profile.
4. Confirm no CommunityUser or StudentLicense is required.

### 4. Current Player Profile Update

1. Authenticate as the B2C player.
2. Call `PATCH /api/v1/PlayerProfiles/me` with:

```json
{
  "age": 10,
  "grade": 5,
  "schoolName": "Optional School Name"
}
```

3. Confirm response returns the updated profile.
4. Call `GET /api/v1/Users/me/player-profile`.
5. Confirm age, grade, and school name persisted.

### 5. Nullable Profile Fields

1. Authenticate as the B2C player.
2. Call `PATCH /api/v1/PlayerProfiles/me` with one or more nullable editable fields set to `null`.
3. Confirm the request succeeds if values otherwise satisfy profile rules.

### 6. Invalid Profile Values

1. Attempt profile update with invalid age or grade.
2. Confirm the request is rejected with the existing project error style.

### 7. B2B Compatibility

1. Run or manually validate existing student activation login.
2. Run or manually validate existing Teacher and Owner login flows.
3. Confirm behavior remains unchanged.

## Expected Non-Changes

- No new database table.
- No migration unless planning later discovers missing PlayerProfile fields.
- No community membership creation.
- No student license creation.
- No real analytics calculation.
- No reports, assignments, parent accounts, or payment behavior.

## Analytics Context Note

- B2C game activity should use `CommunityId = null` in future analytics design.
- B2B school activity should use the relevant `communityId`.
- This feature does not emit or calculate analytics.
