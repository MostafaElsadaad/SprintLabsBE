# Game developer handoff: first-login B2C profile setup

## When to show the screen

1. Complete the existing Firebase/development-account login and obtain the SprintLabs access token.
2. With that bearer token, GET `/api/v1/PlayerProfiles/me/setup`.
3. If `data.isCommunityManaged` is true, skip B2C setup and use the existing community profile flow. Do not allow the B2C form to overwrite B2B data.
4. Otherwise, if `data.requiresSetup` is false, continue to the lobby. Use its nickname as the player name.
5. If true, fetch GET `/api/v1/PlayerProfiles/setup-options`, then show the existing first-time details screen.
6. POST all four fields to `/api/v1/PlayerProfiles/me/setup` when the player confirms. Continue only after HTTP 200 and `data.requiresSetup=false`.
7. Refresh GET `/api/v1/Users/me/player-profile` (and the game's cached player name) so lobby/gameplay uses the chosen nickname rather than the provider's display name. Check setup again on the next login; no local PlayerPrefs completion flag is authoritative.

## Screen fields

| UI field | API field | Rule |
|---|---|---|
| Name / nickname | `nickname` | Required; trim; 1–100 characters; no control characters. This is the game player name. |
| Grade | `grade` | Dropdown from `data.grades`; integer 7–12, not a Grade database ID. Replace mock “Grade 3” from the screenshot. |
| Education | `curriculum` | Distinct curriculum strings from `data.educationOptions`. |
| Language | `language` | Options only from pairs matching the selected curriculum. Clear an incompatible language when curriculum changes. |

No admin tables, extra pages, age, school, address, picture upload or community selector are needed for this form. Existing grade/name values may prefill the form; missing fields remain unselected. Do not silently choose the first grade/curriculum/language for the player.

## Requests

Use the game's centralized backend client/BaseUrl and normal authorization header. Set `Content-Type: application/json` for POST. Paths are relative to the configured API host.

```http
GET /api/v1/PlayerProfiles/me/setup
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
```

```http
GET /api/v1/PlayerProfiles/setup-options
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
```

```http
POST /api/v1/PlayerProfiles/me/setup
Authorization: Bearer <SPRINTLABS_ACCESS_TOKEN>
Content-Type: application/json

{
  "nickname": "Space Explorer",
  "grade": 7,
  "curriculum": "Egyptian National",
  "language": "English"
}
```

Use actual option strings returned by the backend; question-bank curricula/languages can add further choices. Successful responses use `data` inside the usual BaseResponse envelope. Full response/error examples are in [api.md](api.md).

## State and error handling

- While checking login/setup or fetching options, show loading; avoid flashing the setup form for already-complete users.
- On options failure, show retry and preserve any typed nickname. If no valid pairs are returned, disable save and show an unavailable message; do not invent choices.
- Disable Save until every field is valid and while saving. On a network error keep selections and allow retry; repeating the request does not duplicate a player.
- On 400 show invalid-details feedback; refresh options if choices may have changed. Keep the player on the form.
- On 401 use the existing token refresh/login flow. On 403 refresh setup status; the player may now be community-managed. On 404 show account-profile unavailable and allow sign-in/support retry.
- Discard in-flight setup state when the authenticated account changes. Never show the previous account's draft or apply an old response to the new account.
- Do not log tokens, send IDs to choose another player, or report local success before the backend saves.

## Manual acceptance checks

1. New B2C account: requiresSetup=true; choose nickname, grade 7, education/language, save, enter lobby.
2. Restart/sign out/in: requiresSetup=false and the same selected values return; nickname remains the gameplay name.
3. Save grade 6/13, blank nickname or an unlisted education/language pair: 400 and previous values preserved.
4. Save the same valid request twice: same profile, no extra player, XP/gold unchanged.
5. Another account cannot read/change this player's setup by submitting userId/playerProfileId.
6. Active/pending community student: skip B2C form; POST denied with 403.
7. Switch accounts while loading/saving: no draft or result leaks between accounts.

Delivery: B2C and shop implementations are integrated on `codex/coin-shop`; Unity integration remains separate. Example curricula are seeded by the migration. Their availability does not guarantee a populated question bank for every selected grade.
