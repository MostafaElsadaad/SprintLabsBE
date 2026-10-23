# Bundled profile appearance API

Implemented 2026-10-23. The game bundles image assets; the backend stores two independent selected project-relative paths. It does not upload, host, fetch, inspect, or verify the existence of image files. No database or web-server filesystem path is accepted.

## Endpoints and authentication

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/v1/Users/me/player-profile` | Read own profile and selected paths. |
| PATCH | `/api/v1/PlayerProfiles/me` | Update own profile using partial-field semantics. |
| POST | `/api/v1/PlayerProfiles/me/appearance` | Alias for the same update action, for clients with an existing POST transport. |

The two update routes also have `/api/v1/player-profiles/` aliases. All require an authenticated SprintLabs bearer token with a valid `userId` claim. No community membership or role is required. Suspended users cannot read or update their profile. Identity is taken from the authenticated claim; body/query `userId` or `playerProfileId` does not select another player.

## Update body

```json
{
  "avatarAssetPath": "Assets/_Project/Backend_Data/Avatar/test-avatar.png",
  "profilePictureAssetPath": "Assets/_Project/Backend_Data/ProfilePic/test-picture.jpg"
}
```

Each property is optional and independent. An omitted property preserves its stored value; explicit JSON `null` clears it. Both routes use the existing profile request, which also accepts `age`, `grade`, and `schoolName` with their existing nullable update semantics. Empty strings are invalid paths. Progression and identity fields are not editable through this request.

Rules for non-null paths:

- Maximum 512 characters, stored exactly without trimming or normalization.
- `avatarAssetPath` starts with exactly `Assets/_Project/Backend_Data/Avatar/`.
- `profilePictureAssetPath` starts with exactly `Assets/_Project/Backend_Data/ProfilePic/`.
- Roots are case-sensitive. Nested directories are supported.
- Segments contain only Unicode letters/digits, spaces, `.`, `_`, and `-`; no empty segments, `.` or `..` segments, trailing periods, or leading/trailing segment whitespace.
- Final extension is `.png`, `.jpg`, or `.jpeg`, case-insensitive.
- Absolute paths, URLs, backslashes, traversal, percent escapes, query strings, and control characters are rejected.

## Success

All three endpoints return HTTP 200 with the existing `BaseResponse<PlayerProfileResponse>` envelope. Example `data`:

```json
{
  "id": 100,
  "name": "Player",
  "email": "player@example.com",
  "pictureUrl": "https://example.com/legacy-picture.png",
  "avatarAssetPath": "Assets/_Project/Backend_Data/Avatar/test-avatar.png",
  "profilePictureAssetPath": "Assets/_Project/Backend_Data/ProfilePic/test-picture.jpg",
  "gold": 5,
  "experience": 50,
  "level": 2,
  "schoolName": null,
  "age": null,
  "grade": null
}
```

Existing `Players.AvatarUrl` and response `pictureUrl` remain unchanged. Clients resolve bundled selections locally and can retain the legacy picture as fallback. The path fields are selection metadata, never a URL to request from this API. The backend does not guarantee that a particular client build contains a selected asset.

## Common errors

| Status | Meaning |
| --- | --- |
| 400 | Malformed body, non-string/non-null path value, invalid path, or existing profile-field validation failure (`InvalidInput`). |
| 401 | Missing/invalid bearer authentication or missing/unparseable authenticated `userId` claim. |
| 403 | Suspended user (`InvalidAccessToken`). |
| 404 | User or player profile not found (`NotFound`). |

An invalid appearance value prevents the profile update from being persisted. Tracked profile updates write changed fields only, preserving progression awarded concurrently after a profile was loaded. The response reflects the loaded profile plus its edits; refresh for the latest concurrent progression. Never include bearer tokens in logs or examples. Tests authenticate through a test-only scheme and exercise the production authorization attributes, controller, CQRS handlers, repository, and SQLite persistence.

## Schema and rollout

`AddBundledProfileAppearance` adds nullable `Players.AvatarAssetPath` and `Players.ProfilePictureAssetPath`, each `varchar(512)`. Existing rows start with null selections. Apply the migration through the normal backend deployment procedure before serving this API version; migration/deployment was not performed by this change. No new package, project reference, table, repository, image server, or upload endpoint is introduced.
