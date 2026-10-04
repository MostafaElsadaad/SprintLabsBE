# OnlineScene leaderboard integration

Scope: the existing Leaderboard panel and its own-player row. No question, shop, bag, cosmetics, profile editor or rank-ladder changes.

## Calls and controls

- On Global open, fetch `GET /api/v1/Ranking/leaderboard?page=1&pageSize=20&period=AllTime`.
- For Week/Month/Year tabs, send the matching `period`. Year means the current calendar year; AllTime is a distinct choice. There is no Daily or previous-period selector.
- For School, use an authorized community ID from existing `GET /Users/me/communities` and fetch `/Communities/{communityId}/ranking/leaderboard`. Membership discovery alone does not guarantee permission: pending/revoked or otherwise ineligible accounts can receive 403.
- Build permitted Class/Grade options using response `availableFilters`. Deduplicate grades by gradeId. Selecting a class sends its classId and corresponding gradeId; selecting a school-wide board omits both. Players receive only their enrolled filter options, teachers assigned classes, owners/admins eligible classes.
- Reset page to 1 when changing school, period, grade or class. Global requests omit gradeId/classId. Apply the 1–100 page-size bound.
- The list response already contains `currentPlayer`, so opening a screen needs no second request. `/Ranking/leaderboard/me` and the community equivalent support compact own-standing refreshes elsewhere, or when page rows are not needed.

## Rendering

| UI element | Backend field |
| --- | --- |
| Row position | `position`; never derive from local row index |
| Name/avatar | `name`, `avatarUrl`; local fallback for absent avatar |
| Points | `points`; AllTime RP or selected-period net RP |
| Current rank/level | `rankTier`, `level`; do not derive rank from XP or period points |
| Wins if displayed | `totalWins`; lifetime, not period-specific |
| Own pinned row | `currentPlayer`; independent of current page |
| Highlight local row | Match playerProfileId against the current profile ID |
| Period bounds | `periodStartsAt` / `periodEndsAt`, returned in UTC |
| Pagination | `page`, `pageSize`, `total` |

Keep backend Int64 IDs, positions and points as C# long. Negative period points are valid. School/grade are filter context, not per-player educational details in row payloads. Existing sample school/XP row bindings must be adjusted to the actual contract. Immortal behavior is unchanged.

## States and actions

- Show loading on initial fetch/filter switch; preserve cached rows with an explicit refreshing state for background refresh. Handle cancellation and discard stale responses after switching filters/account.
- Show a normal empty state for total=0. If only the page is out of range, return to an available page.
- `currentPlayer=null` on a period board means no qualifying ranked activity; show unranked/no activity, not position zero. Claiming missions doesn't change leaderboard period points.
- Handle 401 by restoring sign-in, 403 by showing unavailable school/class access, 404 by clearing stale school/filter selections, and 400 by correcting parameters. Provide a retry for temporary transport/server failures.
- Refresh after the dedicated server confirms persisted ranked match completion, and on explicit refresh. There are no frontend score updates or writes.
- Use Unity's existing centralized backend transport and explicit composition. UI consumes a domain service/store, with immutable snapshots, rather than direct UnityWebRequest calls. This backend feature does not implement the Unity adapter.

## Manual verification

1. Compare list and own-only standing for the same scope/period/filter, including an own player outside page 1.
2. Verify page 2 retains backend ordinal positions and stable equal-point order.
3. Complete Ranked matches, confirm period net RP ordering; Friendly/Private and mission claims do not add points.
4. Check Week rollover on Monday 00:00 UTC and Month/Year rollover using returned boundaries.
5. Check zero/negative scores and an account without period activity.
6. Verify student school board and own class; reject another class/school, revoked license and inactive class. Verify assigned-teacher restrictions remain intact.
7. Confirm no email/answer/credential data appears in rows or logs. Verify loading/empty/error/refresh handling.
