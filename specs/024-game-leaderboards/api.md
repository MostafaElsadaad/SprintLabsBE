# Game leaderboard API

All routes use `/api/v1` and normal SprintLabs bearer authentication. Caller identity comes from the authenticated `userId` claim; query values cannot select another caller. Requests have no body. Responses use the existing `BaseResponse<T>` envelope. IDs and positions are Int64; `points` is signed Int64.

## Endpoints

| Method | Path | Data |
| --- | --- | --- |
| GET | `/Ranking/leaderboard` | Leaderboard page plus own standing |
| GET | `/Ranking/leaderboard/me` | Own standing and board total without page rows |
| GET | `/Communities/{communityId}/ranking/leaderboard` | Authorized school/class page plus own standing |
| GET | `/Communities/{communityId}/ranking/leaderboard/me` | Own standing in that school/class board |

The two list endpoints already existed; default AllTime behavior and existing row/page fields are preserved. The additions are `points`, period metadata, `currentPlayer` and `availableFilters`. `/Ranking/me` remains a private RP/tier summary and does not become a leaderboard endpoint.

## Query parameters

| Field | Default | Validation/meaning |
| --- | --- | --- |
| `period` | `AllTime` | Named `AllTime`, `Week`, `Month`, `Year`; case-insensitive. No numeric enums, Daily, past-period selector or arbitrary dates |
| `page` | 1 | Positive; `(page - 1) * pageSize` must fit Int32 |
| `pageSize` | 20 | 1â€“100 |
| `gradeId` | absent | Positive backend grade ID; school routes only |
| `classId` | absent | Positive backend class ID; school routes only |

Standing endpoints accept the same validated parameters, but page/pageSize do not affect the position. Grade/class filters intersect. A grade ID is not the displayed grade number. Use IDs from `availableFilters`.

## Scoring and periods

- AllTime: points equal current persisted player RP; every active linked player is eligible, including zero RP. Bounds are null.
- Week/Month/Year: points are the signed sum of saved `RpChange` reward facts from Completed Ranked matches in the current UTC calendar period. Week starts Monday 00:00 UTC; month/year start on their first day. Bounds are `[periodStartsAt, periodEndsAt)`. Use backend `Match.CompletedAt`, not the client's match end timestamp. Future completions beyond the request's server clock instant are excluded.
- Only players with at least one qualifying ranked reward appear on a period board. Zero or negative sums are valid. Friendly/Private matches and mission XP never affect period points. No ranking balance is reset or recalculated by this read API.
- Global periods aggregate all qualifying ranked matches. School periods include only matches registered to that community. AllTime school boards compare eligible members' current lifetime RP, preserving the original behavior.
- Order is points descending, then profile ID ascending. Positions are stable ordinal positions, not shared ranks for ties. The current player's position uses exactly the same eligibility/score/filter rules and is independent of pagination.
- `rp`, `rankTier`, `level` and `totalWins` are current/lifetime profile fields, even on a period board. Use `points` for the selected board's Points column; don't interpret it as current RP or XP.

## Permissions and visibility

All callers must be active backend users. Suspended/unlinked profiles are excluded before sorting, counting and pagination. Global lists work for an active user without a player profile; their currentPlayer is null. Own-only routes return 404 if no player profile exists.

School routes require an active community. Platform admins/owners retain all eligible classes; teachers retain assigned active classes. Active enrolled players can read their school's public standings. Player authorization requires an active student license bound to both their authenticated user and player profile in an active class of that school. Revoked-license, archived-class and suspended-community cases cannot authorize access.

Players may select only grade/class IDs in their own active enrollments. A school's unfiltered board contains public standings across its eligible classes; it exposes no emails, answer data, license details or private progression. Teachers are not granted school-wide access through an enrollment fallback. Private progression/ranking/history permissions are unchanged.

`availableFilters` contains distinct grade/class ID/name pairs: player own enrollments, teacher assigned enrolled classes, owner/admin eligible enrolled classes. It is empty globally. Options do not depend on the selected period or filter; there is no separate student roster query. Empty classes without eligible license rows are not offered.

## Success example

`GET /api/v1/Ranking/leaderboard?period=Week&page=1&pageSize=20`

```json
{
  "data": {
    "items": [
      { "position": 1, "playerProfileId": 101, "name": "Player", "avatarUrl": null,
        "rp": 480, "points": 30, "rankTier": "Seeker", "level": 2, "totalWins": 1 }
    ],
    "page": 1, "pageSize": 20, "total": 1,
    "period": "Week", "periodStartsAt": "2026-10-12T00:00:00Z", "periodEndsAt": "2026-10-19T00:00:00Z",
    "currentPlayer": { "position": 1, "playerProfileId": 101, "name": "Player", "avatarUrl": null,
      "rp": 480, "points": 30, "rankTier": "Seeker", "level": 2, "totalWins": 1 },
    "availableFilters": []
  },
  "message": "Success", "statusCode": 200, "errorCode": 1
}
```

Own-only routes return the same total/period/bounds/currentPlayer/availableFilters, without items/page/pageSize. `currentPlayer` is null when the player does not qualify for the selected period or authorized filter. Valid out-of-range pages return items=[] while retaining total and own standing.

Example school filter option: `{ "gradeId": 1, "gradeName": "Seven", "classId": 5, "className": "A" }`.

## Common errors

| HTTP | Meaning/action |
| --- | --- |
| 400 | Invalid period, page, size, nonpositive ID, overflow offset, global grade/class filter, or malformed query binding; correct parameters |
| 401 | Missing/invalid authentication; sign in again |
| 403 | Suspended caller, no eligible school access, or player selecting another enrolled class/grade; do not fall back to staff routes |
| 404 | Missing/inactive community, foreign/unavailable grade/class filter, or own-only request without a profile |

Use HTTP status and the established error envelope. There are no client-side ranking writes. Don't request another player's private progression to fill leaderboard rows.

## Operational notes

No schema migration, package or configuration change. Period results cover only existing authoritative ranked completion reward records; they cannot reconstruct unrecorded historic play. Current enrollment controls school eligibility, not enrollment at match time. Reads are live queries without a multi-query snapshot guarantee; refresh if a concurrent match changes standings while paging.

SQLite/service/HTTP tests validate behavior and translation. The optional isolated MySQL integration test requires `SPRINTLABS_MYSQL_TEST_CONNECTION`; no staging deployment, real MySQL execution or Unity integration is implied by local test success.
