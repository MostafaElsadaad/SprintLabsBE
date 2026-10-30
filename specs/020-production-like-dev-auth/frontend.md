# Consumer Guide: Production-Like Development Player Authentication

## Scope Boundary

No Unity, Unity Editor, Mirror, browser frontend, page, menu, or session-object change is implemented or planned in this backend feature. This file records only the future consumer contract required by repository documentation rules.

## Future Unity Editor Consumer

A later Unity-only feature may expose a development account selector when its own build/environment policy permits it.

### Data and actions

- Load choices with `GET /api/v1/Account/development-players`.
- Display `displayName`; submit the corresponding opaque `accountKey`.
- Log in with `POST /api/v1/Account/development-login`.
- Pass the returned existing `LoginResponse` into the normal `AuthenticationSession` path.
- Never create, cache as authoritative, or substitute `UserId`/`PlayerProfileId` values.

### States a future consumer must handle

| State | Expected behavior |
|---|---|
| Loading | Prevent duplicate list/login requests and show bounded progress |
| Empty/error | Do not invent fallback accounts; surface that backend development auth is unavailable or misconfigured |
| Unauthorized | Treat as development-key configuration failure without displaying the key |
| Invalid seed state | Do not hide missing players or attempt client-side repair |
| Login success | Continue through existing production session and Mirror authentication flow |
| Login failure | Keep the existing authenticated session unchanged and issue no fake identity |

### Visibility and secrets

- The future selector is an Editor/development concern, not production UI.
- If a configured API key must be supplied by Unity Editor, it must come from an uncommitted local/deployment mechanism; exact Unity secret handling is an open decision for that later feature.
- The key must never be displayed, logged, persisted as a player credential, or sent to Mirror.

## Explicitly Out of Scope

No frontend form fields beyond account selection, profile/rank UI, match flow, progression UI, Mirror change, or production login change belongs to this feature.

## 2026-10-30 - Operator setup for school leaderboard testing

No shipped player-facing form is added. Use an operator HTTP client with a private
platform-admin session and the configured development guard header to POST
`/api/v1/admin/development-players/{accountKey}/school` with communityId/gradeId/classId.
See api.md for the full request, response and errors. Obtain IDs from the existing
administrative school listing and that school's authorized availableFilters/classes;
never interpret a displayed grade number as its backend ID.

Show loading while the request runs; HTTP 200 with alreadyAssigned=false means setup
completed, true means the same assignment already exists. 400 capacity requires a school
with a free seat; 401/403 require the correct operator access; 404 requires enabled
non-Production development authentication and existing active scope; 409 requires explicit
review of the conflicting enrollment/role; 503 can retry the same tuple. Do not report
success until a new player login returns the membership and its own class board succeeds.

In Unity Editor, sign in as the selected development player after setup, open Leaderboard,
select School and Class, then each period; Global remains available. No school badge or
local preference can create membership. Keep all operator credentials out of Unity assets,
client UI, URLs, reports and source. Ordinary students/staff do not see this setup action.
